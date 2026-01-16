namespace SAMA.Web.Services.NotificationChannels;

/// <summary>
/// Attribute to specify the channel type identifier for an INotificationChannelHandler implementation.
/// Used for automatic service registration and discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ChannelTypeAttribute : Attribute
{
    /// <summary>
    /// Gets the channel type identifier (e.g., Slack, Email).
    /// Must match the constant defined in <see cref="Constants.ChannelTypes"/>.
    /// </summary>
    public string ChannelType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelTypeAttribute"/> class with the specified channel type.
    /// </summary>
    /// <param name="channelType">The channel type identifier.</param>
    public ChannelTypeAttribute(string channelType)
    {
        ChannelType = channelType;
    }
}
