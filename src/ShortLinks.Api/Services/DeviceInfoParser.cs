namespace ShortLinks.Api.Services;

/// <summary>Lightweight, dependency-free user-agent parser for click statistics.</summary>
public static class DeviceInfoParser
{
    public static (string DeviceType, string Browser) Parse(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return ("unknown", "unknown");
        }

        var ua = userAgent;

        var device = DetectDevice(ua);
        var browser = DetectBrowser(ua);

        return (device, browser);
    }

    private static string DetectDevice(string ua)
    {
        if (ua.Contains("Googlebot", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("bingbot", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("YandexBot", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("Twitterbot", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("facebookexternalhit", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("TelegramBot", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("WhatsApp/", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("curl", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("PostmanRuntime", StringComparison.OrdinalIgnoreCase))
        {
            return "bot";
        }

        if (ua.Contains("Mobi", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return "mobile";
        }

        if (ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
        {
            return "tablet";
        }

        return "desktop";
    }

    private static string DetectBrowser(string ua)
    {
        if (ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
        {
            return "Edge";
        }
        if (ua.Contains("OPR/", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("Opera", StringComparison.OrdinalIgnoreCase))
        {
            return "Opera";
        }
        if (ua.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
        {
            return "Firefox";
        }
        if (ua.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
        {
            return "Chrome";
        }
        if (ua.Contains("Safari/", StringComparison.OrdinalIgnoreCase))
        {
            return "Safari";
        }
        if (ua.Contains("MSIE", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("Trident/", StringComparison.OrdinalIgnoreCase))
        {
            return "Internet Explorer";
        }
        return "other";
    }
}