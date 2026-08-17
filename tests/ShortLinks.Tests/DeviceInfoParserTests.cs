using ShortLinks.Api.Services;

namespace ShortLinks.Tests;

public class DeviceInfoParserTests
{
    [Fact]
    public void Parse_MobileUa_ReturnsMobile()
    {
        var ua = "Mozilla/5.0 (Linux; Android 12; Pixel 5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Mobile Safari/537.36";
        var (device, browser) = DeviceInfoParser.Parse(ua);
        Assert.Equal("mobile", device);
        Assert.Equal("Chrome", browser);
    }

    [Fact]
    public void Parse_DesktopChrome_ReturnsDesktopChrome()
    {
        var ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0 Safari/537.36";
        var (device, browser) = DeviceInfoParser.Parse(ua);
        Assert.Equal("desktop", device);
        Assert.Equal("Chrome", browser);
    }

    [Fact]
    public void Parse_Edge_ReturnsEdge()
    {
        var ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/122.0 Safari/537.36";
        var (device, browser) = DeviceInfoParser.Parse(ua);
        Assert.Equal("Edge", browser);
    }

    [Fact]
    public void Parse_Iphone_ReturnsMobileSafari()
    {
        var ua = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1";
        var (device, browser) = DeviceInfoParser.Parse(ua);
        Assert.Equal("mobile", device);
        Assert.Equal("Safari", browser);
    }

    [Fact]
    public void Parse_Bot_ReturnsBot()
    {
        var ua = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
        var (device, browser) = DeviceInfoParser.Parse(ua);
        Assert.Equal("bot", device);
    }

    [Fact]
    public void Parse_Null_ReturnsUnknown()
    {
        var (device, browser) = DeviceInfoParser.Parse(null);
        Assert.Equal("unknown", device);
        Assert.Equal("unknown", browser);
    }
}