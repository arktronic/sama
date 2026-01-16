using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMA.Web.Extensions;
using SAMA.Web.Models;
using SAMA.Web.Services;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Workspaces;

[AllowAnonymous]
public class IndexModel(
    WorkspaceQueryService _workspaceQueryService,
    WorkspaceAuthorizationService _authorizationService) : PageModel
{
    public IList<WorkspaceDetailsViewModel> Workspaces { get; set; } = [];

    public bool IsAdmin { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.GetUserId();

        IsAdmin = await _authorizationService.IsGlobalAdmin(userId);

        var accessibleWorkspaceIds = await _authorizationService.GetAccessibleWorkspaceIds(userId);
        Workspaces = await _workspaceQueryService.GetWorkspacesAsync(accessibleWorkspaceIds);
    }
}
