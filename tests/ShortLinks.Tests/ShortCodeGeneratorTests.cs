using ShortLinks.Api.Services;

namespace ShortLinks.Tests;

public class ShortCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsBase62CodeOfConfiguredLength()
    {
        var generator = new ShortCodeGenerator { Length = 6 };
        var code = generator.Generate();

        Assert.Equal(6, code.Length);
        Assert.Matches("^[0-9A-Za-z]+$", code);
    }

    [Fact]
    public void Generate_IsHighlyCollisionResistant()
    {
        var generator = new ShortCodeGenerator();
        var generated = Enumerable.Range(0, 10_000).Select(_ => generator.Generate());

        Assert.Equal(10_000, generated.Distinct().Count());
    }

    [Theory]
    [InlineData("aB72x", true)]
    [InlineData("ABC123", true)]
    [InlineData("ab", false)]
    [InlineData("with-dash", false)]
    [InlineData("with space", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidCustomCode_ValidatesFormat(string? code, bool expected)
    {
        Assert.Equal(expected, ShortCodeGenerator.IsValidCustomCode(code));
    }
}