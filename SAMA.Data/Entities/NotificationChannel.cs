using System.Text.Json;

namespace SAMA.Data.Entities;

public class NotificationChannel
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public required string Name { get; set; }

    public required string ChannelType { get; set; } // Email, Slack, Teams, Discord, Script, EventGrid

    public required Dictionary<string, JsonElement> ConfigurationJson { get; set; } // Encrypted JSON

    public bool Enabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }


    // Navigation properties
    public Workspace Workspace { get; set; } = null!;

    public ICollection<EventSubscription> EventSubscriptions { get; set; } = [];

    public ICollection<Alert> Alerts { get; set; } = [];

    public ICollection<AlertHistory> AlertHistories { get; set; } = [];
}
