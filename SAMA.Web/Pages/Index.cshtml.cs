using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMA.Web.Extensions;
using SAMA.Web.Services;

namespace SAMA.Web.Pages;

[AllowAnonymous]
public class IndexModel(WorkspaceAuthorizationService _authorizationService) : PageModel
{
    public bool IsAuthenticated { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        IsAuthenticated = userId.HasValue;

        var accessibleWorkspaceIds = await _authorizationService.GetAccessibleWorkspaceIds(userId);

        if (accessibleWorkspaceIds.Count == 1)
        {
            return RedirectToPage("/Dashboard/Index", new { workspaceId = accessibleWorkspaceIds[0] });
        }

        if (accessibleWorkspaceIds.Count > 1)
        {
            return RedirectToPage("/Workspaces/Index");
        }

        // No accessible workspaces - show landing page
        return Page();
    }
}
