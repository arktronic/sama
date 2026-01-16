using Microsoft.AspNetCore.Mvc.Rendering;
using SAMA.Web.Services;

namespace SAMA.Web.Extensions;

/// <summary>
/// Extension methods for use in Razor views.
/// </summary>
public static class ViewExtensions
{
    /// <summary>
    /// Checks if the current user can edit the specified workspace.
    /// First checks ViewData["CanEdit"] (set by WorkspacePageModel) to avoid redundant queries.
    /// Falls back to querying WorkspaceAuthorizationService if not available.
    /// </summary>
    /// <param name="htmlHelper">The HTML helper.</param>
    /// <param name="workspaceId">The workspace ID to check.</param>
    /// <returns>True if the user can edit the workspace, false otherwise.</returns>
    public static async Task<bool> CanEditWorkspaceAsync(this IHtmlHelper htmlHelper, Guid workspaceId)
    {
        var httpContext = htmlHelper.ViewContext.HttpContext;

        // Fast path: Check if WorkspacePageModel already set this
        var viewData = htmlHelper.ViewData;
        if (viewData["CanEdit"] is bool knownCanEdit &&
            viewData["WorkspaceId"]?.ToString() == workspaceId.ToString("D"))
        {
            return knownCanEdit;
        }

        // Slow path: Query the service (for partials or non-WorkspacePageModel pages)
        var user = httpContext.User;
        var userId = user.GetUserId();
        if (!userId.HasValue)
        {
            return false;
        }

        var authService = httpContext.RequestServices.GetRequiredService<WorkspaceAuthorizationService>();
        return await authService.CanEditWorkspace(userId.Value, workspaceId);
    }
}
