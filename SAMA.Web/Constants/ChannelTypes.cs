namespace SAMA.Web.Constants;

/// <summary>
/// Notification channel type constants.
/// Defines all supported channel types for alerts and event subscriptions.
/// </summary>
public static class ChannelTypes
{
    /// <summary>
    /// Email notification via SMTP.
    /// </summary>
    public const string Email = "ChannelType_Email";

    /// <summary>
    /// Slack notification via webhook.
    /// </summary>
    public const string Slack = "ChannelType_Slack";

    /// <summary>
    /// Microsoft Teams notification via webhook.
    /// </summary>
    public const string Teams = "ChannelType_Teams";

    /// <summary>
    /// Discord notification via webhook.
    /// </summary>
    public const string Discord = "ChannelType_Discord";

    /// <summary>
    /// Custom script execution for notifications.
    /// </summary>
    public const string Script = "ChannelType_Script";

    /// <summary>
    /// Azure Event Grid notification.
    /// </summary>
    public const string EventGrid = "ChannelType_EventGrid";

    /// <summary>
    /// Gets an array containing the names of all supported notification channel types.
    /// </summary>
    public static readonly string[] AllChannelTypes =
    [
        Email,
        Slack,
        Teams,
        Discord,
        Script,
        EventGrid
    ];

    /// <summary>
    /// Gets the short display name for badges and compact UI elements.
    /// </summary>
    /// <param name="channelType">The channel type constant value</param>
    /// <returns>Short user-friendly display name</returns>
    public static string GetShortDisplayName(string channelType)
    {
        return channelType switch
        {
            Email => "Email",
            Slack => "Slack",
            Teams => "Teams",
            Discord => "Discord",
            Script => "Script",
            EventGrid => "EventGrid",
            _ => channelType
        };
    }

    /// <summary>
    /// Gets the full display name for dropdowns and detailed views.
    /// </summary>
    /// <param name="channelType">The channel type constant value</param>
    /// <returns>Full user-friendly display name with additional context</returns>
    public static string GetFullDisplayName(string channelType)
    {
        return channelType switch
        {
            Email => "Email (SMTP)",
            Slack => "Slack",
            Teams => "Microsoft Teams",
            Discord => "Discord",
            Script => "Script (Custom)",
            EventGrid => "Azure Event Grid",
            _ => channelType
        };
    }
}
