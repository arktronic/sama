namespace SAMA.Web.Models;

public class WorkspaceAssignmentViewModel
{
    public Guid WorkspaceId { get; set; }

    public string WorkspaceName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsAssigned { get; set; }
}
