using ShortLinks.Api.Services;

namespace ShortLinks.Tests;

public class UtmAppenderTests
{
    [Fact]
    public void Append_PlainUrl_WithNoParams_ReturnsUnchanged()
    {
        var url = "https://map.sabzevar.ir/A/B/C?Q=1&W=2";
        Assert.Equal(url, UtmAppender.Append(url, null, null));
    }

    [Fact]
    public void Append_GroupParams_AreMergedIntoQueryString()
    {
        var url = UtmAppender.Append(
            "https://map.sabzevar.ir/A/B/C?Q=1&W=2",
            new Dictionary<string, string> { ["utm_source"] = "WWW" },
            null);

        Assert.Equal("https://map.sabzevar.ir/A/B/C?Q=1&W=2&utm_source=WWW", url);
    }

    [Fact]
    public void Append_GroupParams_AddsQueryStringToUrlWithoutOne()
    {
        var url = UtmAppender.Append(
            "https://example.com/page",
            new Dictionary<string, string> { ["utm_source"] = "WWW" },
            null);

        Assert.Equal("https://example.com/page?utm_source=WWW", url);
    }

    [Fact]
    public void Append_GroupOverridesExistingParamOnSharedUrl()
    {
        var url = UtmAppender.Append(
            "https://example.com/page?utm_source=OLD",
            new Dictionary<string, string> { ["utm_source"] = "NEW" },
            null);

        Assert.Equal("https://example.com/page?utm_source=NEW", url);
    }

    [Fact]
    public void Append_CallerQueryString_WinsOverEverything()
    {
        var url = UtmAppender.Append(
            "https://example.com/page?utm_source=OLD",
            new Dictionary<string, string> { ["utm_source"] = "GROUP" },
            "utm_source=CALLER&foo=bar");

        Assert.Equal("https://example.com/page?utm_source=CALLER&foo=bar", url);
    }

    [Fact]
    public void Append_HandlesExistingQueryWithEncodedChars()
    {
        var url = UtmAppender.Append(
            "https://example.com/a?name=%D8%B3%D8%A8%D8%B2%D9%88%D8%A7%D8%B1=1",
            new Dictionary<string, string> { ["utm_campaign"] = "x" },
            null);

        Assert.Equal("https://example.com/a?name=%D8%B3%D8%A8%D8%B2%D9%88%D8%A7%D8%B1%3D1&utm_campaign=x", url);
    }
}