using System.Text.Json;

namespace SAMA.Web.Models;

public class ChannelDetailsViewModel
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string WorkspaceName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ChannelType { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the total number of alerts that use this channel when triggered.
    /// Includes both alerts explicitly configured to use this channel,
    /// and alerts with no specific channels (which use all workspace channels).
    /// </summary>
    public int AlertCount { get; set; }

    public int EventSubscriptionCount { get; set; }

    public Dictionary<string, object> MaskedConfiguration { get; set; } = [];

    public Dictionary<string, JsonElement> ConfigurationJson { get; set; } = [];
}
