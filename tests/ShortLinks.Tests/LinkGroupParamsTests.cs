using ShortLinks.Api.Domain;

namespace ShortLinks.Tests;

public class LinkGroupParamsTests
{
    [Fact]
    public void SetGetUtmParams_RoundTrips()
    {
        var group = new LinkGroup { Name = "utm" };
        var utm = new Dictionary<string, string>
        {
            ["utm_source"] = "WWW",
            ["utm_campaign"] = "sabzevar campaign",
        };
        group.SetUtmParams(utm);

        var back = group.GetUtmParams();
        Assert.Equal(utm, back);
    }

    [Fact]
    public void GetUtmParams_OnDefaultJson_ReturnsEmpty()
    {
        var group = new LinkGroup { Name = "empty" };
        Assert.Empty(group.GetUtmParams());
    }
}