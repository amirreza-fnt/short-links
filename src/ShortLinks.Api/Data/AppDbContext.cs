using Microsoft.EntityFrameworkCore;
using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ShortLink> ShortLinks => Set<ShortLink>();

    public DbSet<LinkGroup> LinkGroups => Set<LinkGroup>();

    public DbSet<ClickStat> ClickStats => Set<ClickStat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortLink>(e =>
        {
            e.ToTable("ShortLinks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.TargetUrl).HasMaxLength(2048).IsRequired();

            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.ExpiresAt);
            e.HasIndex(x => x.CreatedAt);

            e.HasOne(x => x.Group)
                .WithMany(g => g.Links)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LinkGroup>(e =>
        {
            e.ToTable("LinkGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(64).IsRequired();
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.UtmParamsJson).HasMaxLength(2048).IsRequired();

            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ClickStat>(e =>
        {
            e.ToTable("ClickStats");
            e.HasKey(x => x.Id);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(512);
            e.Property(x => x.DeviceType).HasMaxLength(32);
            e.Property(x => x.Browser).HasMaxLength(64);
            e.Property(x => x.Referrer).HasMaxLength(2048);
            e.Property(x => x.UtmTemplate).HasMaxLength(64);
            e.Property(x => x.QueryString).HasMaxLength(1024);

            e.HasIndex(x => new { x.ShortLinkId, x.ClickedAt });
            e.HasOne(x => x.ShortLink)
                .WithMany(l => l.ClickStats)
                .HasForeignKey(x => x.ShortLinkId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}