namespace SAMA.Shared.Constants;

/// <summary>
/// Check type constants.
/// Defines all supported monitoring check types.
/// </summary>
public static class CheckTypes
{
    /// <summary>
    /// HTTP/HTTPS endpoint monitoring.
    /// </summary>
    public const string Http = "CheckType_HTTP";

    /// <summary>
    /// TCP port connectivity check.
    /// </summary>
    public const string Tcp = "CheckType_TCP";

    /// <summary>
    /// ICMP ping check.
    /// </summary>
    public const string Ping = "CheckType_Ping";

    /// <summary>
    /// DNS resolution check.
    /// </summary>
    public const string Dns = "CheckType_DNS";

    /// <summary>
    /// TLS certificate validation check.
    /// </summary>
    public const string Tls = "CheckType_TLS";

    /// <summary>
    /// Custom script execution check.
    /// </summary>
    public const string Script = "CheckType_Script";

    /// <summary>
    /// All supported check types.
    /// </summary>
    public static readonly string[] AllCheckTypes =
    {
        Http,
        Tcp,
        Ping,
        Dns,
        Tls,
        Script
    };

    /// <summary>
    /// Gets the short display name for badges and compact UI elements.
    /// </summary>
    /// <param name="checkType">The check type constant value</param>
    /// <returns>Short user-friendly display name</returns>
    public static string GetShortDisplayName(string checkType)
    {
        return checkType switch
        {
            Http => "HTTP",
            Tcp => "TCP",
            Ping => "Ping",
            Dns => "DNS",
            Tls => "TLS",
            Script => "Script",
            _ => checkType
        };
    }

    /// <summary>
    /// Gets the full display name for dropdowns and detailed views.
    /// </summary>
    /// <param name="checkType">The check type constant value</param>
    /// <returns>Full user-friendly display name with additional context</returns>
    public static string GetFullDisplayName(string checkType)
    {
        return checkType switch
        {
            Http => "HTTP/HTTPS",
            Tcp => "TCP Port",
            Ping => "Ping (ICMP)",
            Dns => "DNS Lookup",
            Tls => "TLS Certificate",
            Script => "Script (Custom)",
            _ => checkType
        };
    }
}
