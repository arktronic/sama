namespace SAMA.Web.Models;

public class AlertEditViewModel
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

    public List<Guid> SelectedChannelIds { get; set; } = [];
}
