namespace SAMA.Web.Models;

public class CheckBasicInfoViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid WorkspaceId { get; set; }

    public string WorkspaceName { get; set; } = string.Empty;
}
