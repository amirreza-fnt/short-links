using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ShortLinks.Api.Data;
using ShortLinks.Api.Domain;
using ShortLinks.Api.Dtos;

namespace ShortLinks.Api.Services;

public sealed partial class LinkManagementService(
    AppDbContext db,
    CacheService cache,
    ShortCodeGenerator codeGenerator,
    ILogger<LinkManagementService> logger)
{
    public async Task<LinkDto> CreateAsync(
        CreateLinkRequest request,
        string baseUrl,
        CancellationToken ct = default)
    {
        if (!UrlNormalizer.TryNormalize(request.Url, out var normalizedUrl, out var urlError))
        {
            throw new AppValidationException(urlError ?? "Invalid URL.");
        }

        string code = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            code = request.Code.Trim();
            if (!ShortCodeGenerator.IsValidCustomCode(code))
            {
                throw new AppValidationException("Code must be 3-32 alphanumeric characters.");
            }
        }

        if (code.Length > 0)
        {
            var exists = await db.ShortLinks.AnyAsync(l => l.Code == code, ct);
            if (exists)
            {
                throw new AppConflictException($"Code '{code}' is already in use.");
            }
        }

        LinkGroup? group = null;
        if (!string.IsNullOrWhiteSpace(request.GroupName))
        {
            group = await db.LinkGroups.FirstOrDefaultAsync(g => g.Name == request.GroupName, ct);
            if (group is null)
            {
                throw new AppValidationException($"Group '{request.GroupName}' does not exist.");
            }
        }

        var now = DateTimeOffset.UtcNow;

        if (code.Length == 0)
        {
            // Collision-avoidance loop for random codes.
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var candidate = codeGenerator.Generate();
                var collision = await db.ShortLinks.AnyAsync(l => l.Code == candidate, ct);
                if (!collision)
                {
                    code = candidate;
                    break;
                }
            }
            if (code.Length == 0)
            {
                throw new AppValidationException("Could not generate a unique code. Try again.");
            }
        }

        var entity = new ShortLink
        {
            Code = code,
            TargetUrl = normalizedUrl,
            GroupId = group?.Id,
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = request.ExpiresAt,
            IsActive = request.IsActive,
        };

        db.ShortLinks.Add(entity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created short link {Code} -> {Target}", code, normalizedUrl);
        return ToDto(entity, group?.Name, baseUrl);
    }

    public async Task<LinkDto?> GetAsync(string code, string baseUrl, CancellationToken ct = default)
    {
        var entity = await db.ShortLinks
            .AsNoTracking()
            .Include(l => l.Group)
            .FirstOrDefaultAsync(l => l.Code == code, ct);

        return entity is null ? null : ToDto(entity, entity.Group?.Name, baseUrl);
    }

    public async Task<PagedResult<LinkDto>> GetListAsync(
        string? search,
        string? groupName,
        int page,
        int pageSize,
        string baseUrl,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.ShortLinks.AsNoTracking().Include(l => l.Group).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l =>
                l.Code.Contains(term) || l.TargetUrl.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(groupName))
        {
            query = query.Where(l => l.Group != null && l.Group.Name == groupName);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<LinkDto>(
            items.Select(l => ToDto(l, l.Group?.Name, baseUrl)).ToList(),
            total,
            page,
            pageSize);
    }

    public async Task<LinkDto?> UpdateAsync(
        string code,
        UpdateLinkRequest request,
        string baseUrl,
        CancellationToken ct = default)
    {
        var entity = await db.ShortLinks
            .Include(l => l.Group)
            .FirstOrDefaultAsync(l => l.Code == code, ct);
        if (entity is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Url))
        {
            if (!UrlNormalizer.TryNormalize(request.Url, out var normalizedUrl, out var urlError))
            {
                throw new AppValidationException(urlError ?? "Invalid URL.");
            }
            entity.TargetUrl = normalizedUrl;
        }

        var finalGroupName = entity.Group?.Name;

        if (request.GroupName is { } groupName)
        {
            if (groupName.Length == 0)
            {
                entity.GroupId = null;
                finalGroupName = null;
            }
            else
            {
                var group = await db.LinkGroups.FirstOrDefaultAsync(g => g.Name == groupName, ct);
                if (group is null)
                {
                    throw new AppValidationException($"Group '{groupName}' does not exist.");
                }
                entity.GroupId = group.Id;
                finalGroupName = group.Name;
            }
        }

        if (request.ExpiresAt is { } expiresAt)
        {
            entity.ExpiresAt = expiresAt;
        }

        if (request.IsActive is { } isActive)
        {
            entity.IsActive = isActive;
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await InvalidateLinkCacheAsync(code, ct);

        logger.LogInformation("Updated short link {Code}", code);
        return ToDto(entity, finalGroupName, baseUrl);
    }

    public async Task<bool> DeleteAsync(string code, CancellationToken ct = default)
    {
        var entity = await db.ShortLinks.FirstOrDefaultAsync(l => l.Code == code, ct);
        if (entity is null)
        {
            return false;
        }

        db.ShortLinks.Remove(entity);
        await db.SaveChangesAsync(ct);

        await InvalidateLinkCacheAsync(code, ct);
        logger.LogInformation("Deleted short link {Code}", code);
        return true;
    }

    private static LinkDto ToDto(ShortLink l, string? groupName, string baseUrl)
    {
        var shortUrl = string.IsNullOrEmpty(groupName)
            ? $"{baseUrl.TrimEnd('/')}/{l.Code}"
            : $"{baseUrl.TrimEnd('/')}/{groupName}/{l.Code}";

        return new LinkDto
        {
            Id = l.Id,
            Code = l.Code,
            ShortUrl = shortUrl,
            TargetUrl = l.TargetUrl,
            GroupName = groupName,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt,
            ExpiresAt = l.ExpiresAt,
            IsActive = l.IsActive,
            ClickCount = l.ClickCount,
            LastRedirectAt = l.LastRedirectAt,
        };
    }

    public async Task InvalidateLinkCacheAsync(string code, CancellationToken ct = default)
    {
        await cache.RemoveAsync(CacheKeys.Link(code), ct);
    }
}

public sealed partial class GroupManagementService(
    AppDbContext db,
    CacheService cache,
    ILogger<GroupManagementService> logger)
{
    public async Task<GroupDto> CreateAsync(CreateGroupRequest request, string baseUrl, CancellationToken ct = default)
    {
        var name = request.Name.Trim();

        if (!ValidGroupNameRegex().IsMatch(name) || name.Length is < 1 or > 64)
        {
            throw new AppValidationException("Group name must be 1-64 characters (letters, digits, '-' and '_').");
        }

        if (await db.LinkGroups.AnyAsync(g => g.Name == name, ct))
        {
            throw new AppConflictException($"Group '{name}' already exists.");
        }

        var entity = new LinkGroup
        {
            Name = name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        entity.SetUtmParams(request.UtmParams ?? new Dictionary<string, string>());

        db.LinkGroups.Add(entity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created UTM group {Name}", name);
        return ToDto(entity, 0, baseUrl);
    }

    public async Task<GroupDto?> GetAsync(string name, string baseUrl, CancellationToken ct = default)
    {
        var entity = await db.LinkGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Name == name, ct);
        if (entity is null)
        {
            return null;
        }
        var linkCount = await db.ShortLinks.CountAsync(l => l.GroupId == entity.Id, ct);
        return ToDto(entity, linkCount, baseUrl);
    }

    public async Task<List<GroupDto>> GetListAsync(string baseUrl, CancellationToken ct = default)
    {
        var groups = await db.LinkGroups.AsNoTracking().OrderBy(g => g.Name).ToListAsync(ct);
        var linksPerGroup = await db.ShortLinks
            .Where(l => l.GroupId != null)
            .GroupBy(l => l.GroupId!.Value)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        return groups.Select(g =>
                ToDto(g, linksPerGroup.GetValueOrDefault(g.Id), baseUrl))
            .ToList();
    }

    public async Task<GroupDto?> UpdateAsync(
        string name,
        UpdateGroupRequest request,
        string baseUrl,
        CancellationToken ct = default)
    {
        var entity = await db.LinkGroups.FirstOrDefaultAsync(g => g.Name == name, ct);
        if (entity is null)
        {
            return null;
        }

        if (request.Description is not null)
        {
            entity.Description = request.Description;
        }

        if (request.UtmParams is not null)
        {
            entity.SetUtmParams(request.UtmParams);
        }

        if (request.IsActive is { } isActive)
        {
            entity.IsActive = isActive;
        }

        await db.SaveChangesAsync(ct);

        await cache.RemoveAsync(CacheKeys.Group(name), ct);

        var linkCount = await db.ShortLinks.CountAsync(l => l.GroupId == entity.Id, ct);
        return ToDto(entity, linkCount, baseUrl);
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken ct = default)
    {
        var entity = await db.LinkGroups.FirstOrDefaultAsync(g => g.Name == name, ct);
        if (entity is null)
        {
            return false;
        }

        var inUse = await db.ShortLinks.AnyAsync(l => l.GroupId == entity.Id, ct);
        if (inUse)
        {
            throw new AppConflictException($"Group '{name}' is used by one or more links. Unassign them first.");
        }

        db.LinkGroups.Remove(entity);
        await db.SaveChangesAsync(ct);

        await cache.RemoveAsync(CacheKeys.Group(name), ct);
        return true;
    }

    private static GroupDto ToDto(LinkGroup g, long linkCount, string baseUrl)
    {
        var templateUrl = g.IsActive && g.GetUtmParams().Count > 0
            ? $"{baseUrl.TrimEnd('/')}/{g.Name}/{{code}}"
            : string.Empty;

        return new GroupDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            UtmParams = g.GetUtmParams(),
            IsActive = g.IsActive,
            CreatedAt = g.CreatedAt,
            LinkCount = linkCount,
            TemplateUrl = templateUrl,
        };
    }

    [GeneratedRegex(@"^[A-Za-z0-9_-]+$")]
    private static partial Regex ValidGroupNameRegex();
}