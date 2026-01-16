namespace SAMA.Web.Constants;

/// <summary>
/// Event type constants for event subscriptions.
/// Defines lifecycle and status change events that channels can subscribe to.
/// </summary>
public static class EventTypes
{
    /// <summary>
    /// Event fired when a new check is created.
    /// </summary>
    public const string CheckCreated = "CheckCreated";

    /// <summary>
    /// Event fired when a check configuration is updated.
    /// </summary>
    public const string CheckUpdated = "CheckUpdated";

    /// <summary>
    /// Event fired when a check is deleted.
    /// </summary>
    public const string CheckDeleted = "CheckDeleted";

    /// <summary>
    /// Event fired when a check status transitions between states.
    /// Examples: Up→Warn, Up→Down, Warn→Down, Down→Warn, Warn→Up, Down→Up
    /// </summary>
    public const string CheckStatusChanged = "CheckStatusChanged";

    /// <summary>
    /// All defined event types.
    /// </summary>
    public static readonly string[] AllEventTypes =
    [
        CheckCreated,
        CheckUpdated,
        CheckDeleted,
        CheckStatusChanged
    ];

    /// <summary>
    /// Gets the user-friendly display name for an event type.
    /// </summary>
    /// <param name="eventType">The event type constant value</param>
    /// <returns>User-friendly display name with proper spacing</returns>
    public static string GetDisplayName(string eventType)
    {
        return eventType switch
        {
            CheckCreated => "Check Created",
            CheckUpdated => "Check Updated",
            CheckDeleted => "Check Deleted",
            CheckStatusChanged => "Check Status Changed",
            _ => eventType
        };
    }
}
