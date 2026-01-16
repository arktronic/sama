using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SAMA.Web.Extensions;
using SAMA.Web.Services;

namespace SAMA.Web.Authorization;

/// <summary>
/// Requires workspace view access for the current user.
/// Workspace ID must be provided via route parameter 'workspaceId' or check's WorkspaceId.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireWorkspaceViewAccessAttribute : Attribute, IAsyncAuthorizationFilter, IAllowAnonymous
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var authService = context.HttpContext.RequestServices
            .GetRequiredService<WorkspaceAuthorizationService>();

        var userId = context.HttpContext.User.GetUserId();
        var workspaceId = await AuthHelpers.GetWorkspaceIdAsync(context);

        if (!workspaceId.HasValue)
        {
            context.Result = new RedirectToPageResult("/Workspaces/Index");
            return;
        }

        if (!await authService.CanViewWorkspace(userId, workspaceId.Value))
        {
            if (!userId.HasValue)
            {
                context.Result = new RedirectToPageResult("/Account/Login");
                return;
            }

            context.Result = new RedirectToPageResult("/Account/AccessDenied");
        }
    }
}
