namespace SAMA.Web.Models;

public class AlertDetailsViewModel
{
    public Guid Id { get; set; }

    public Guid CheckId { get; set; }

    public string CheckName { get; set; } = string.Empty;

    public Guid WorkspaceId { get; set; }

    public string WorkspaceName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool TriggerOnWarn { get; set; }

    public bool TriggerOnDown { get; set; }

    public int FailureThreshold { get; set; }

    public bool SendRecoveryNotification { get; set; }

    public bool Enabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<ChannelInfo> Channels { get; set; } = [];

    public int AlertHistoryCount { get; set; }

    public class ChannelInfo
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ChannelType { get; set; } = string.Empty;

        public bool Enabled { get; set; }
    }
}
