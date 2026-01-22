using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMA.Data.Entities;
using SAMA.Web.Extensions;
using SAMA.Web.Services;

namespace SAMA.Web.Pages;

[AllowAnonymous]
public class IndexModel(
    WorkspaceAuthorizationService _authorizationService,
    UserPreferencesService _userPreferences,
    GlobalSettingsService _globalSettings,
    UserManager<ApplicationUser> _userManager) : PageModel
{
    public bool IsAuthenticated { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetUserId();
        IsAuthenticated = userId.HasValue;

        var accessibleWorkspaceIds = await _authorizationService.GetAccessibleWorkspaceIds(userId);

        // Check for user's default workspace preference
        if (userId.HasValue)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var defaultWorkspaceId = await _userPreferences.GetDefaultWorkspaceIdAsync(user);
                if (defaultWorkspaceId.HasValue && accessibleWorkspaceIds.Contains(defaultWorkspaceId.Value))
                {
                    return RedirectToPage("/Dashboard/Index", new { workspaceId = defaultWorkspaceId.Value });
                }
            }
        }
        else
        {
            // For anonymous users, check the global default workspace setting
            var anonymousDefaultWorkspaceId = _globalSettings.AnonymousDefaultWorkspaceId;
            if (anonymousDefaultWorkspaceId.HasValue && accessibleWorkspaceIds.Contains(anonymousDefaultWorkspaceId.Value))
            {
                return RedirectToPage("/Dashboard/Index", new { workspaceId = anonymousDefaultWorkspaceId.Value });
            }
        }

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
