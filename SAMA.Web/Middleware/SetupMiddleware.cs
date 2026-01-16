using SAMA.Web.Services;

namespace SAMA.Web.Middleware;

/// <summary>
/// Middleware that redirects to the setup page if initial setup is required.
/// </summary>
public class SetupMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPaths = ["/setup", "/lib/", "/css/", "/js/"];

    public async Task InvokeAsync(HttpContext context, SetupService setupService)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Allow setup page and static resources
        foreach (var allowedPath in AllowedPaths)
        {
            if (path.StartsWith(allowedPath))
            {
                await next(context);
                return;
            }
        }

        // Check if setup is required
        if (await setupService.IsSetupRequiredAsync())
        {
            context.Response.Redirect("/Setup");
            return;
        }

        await next(context);
    }
}
