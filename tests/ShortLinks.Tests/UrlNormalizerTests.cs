using ShortLinks.Api.Services;

namespace ShortLinks.Tests;

public class UrlNormalizerTests
{
    [Theory]
    [InlineData("https://example.com/page?a=1", true, "https://example.com/page?a=1")]
    [InlineData("example.com/page", true, "https://example.com/page")]
    [InlineData(" HTTP://EXAMPLE.com/x ", true, "http://example.com/x")]
    [InlineData("ftp://example.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("not a url", false)]
    [InlineData("javascript:alert(1)", false)]
    public void TryNormalize_BehavesAsExpected(string? input, bool expectedOk, string? expectedUrl = null)
    {
        var ok = UrlNormalizer.TryNormalize(input, out var normalized, out var error);

        Assert.Equal(expectedOk, ok);
        if (expectedOk)
        {
            Assert.Equal(expectedUrl, normalized);
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }
}