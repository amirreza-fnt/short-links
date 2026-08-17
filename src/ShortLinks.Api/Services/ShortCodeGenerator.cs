using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ShortLinks.Api.Services;

/// <summary>Generates collision-resistant, human-friendly short codes (base62).</summary>
public sealed partial class ShortCodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public const int DefaultLength = 6;
    public const int MinLength = 3;
    public const int MaxLength = 32;

    public int Length { get; init; } = DefaultLength;

    public string Generate()
    {
        var buffer = new byte[Length];
        RandomNumberGenerator.Fill(buffer);
        var sb = new StringBuilder(Length);
        foreach (var b in buffer)
        {
            sb.Append(Alphabet[b % Alphabet.Length]);
        }
        return sb.ToString();
    }

    public static bool IsValidCustomCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }
        return ValidCodeRegex().IsMatch(code) &&
               code.Length >= MinLength &&
               code.Length <= MaxLength;
    }

    [GeneratedRegex(@"^[0-9A-Za-z]+$")]
    private static partial Regex ValidCodeRegex();
}