namespace SAMA.Web.Models;

public class AlertListItemViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool TriggerOnWarn { get; set; }

    public bool TriggerOnDown { get; set; }

    public int FailureThreshold { get; set; }

    public bool SendRecoveryNotification { get; set; }

    public bool Enabled { get; set; }

    public int ChannelCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
