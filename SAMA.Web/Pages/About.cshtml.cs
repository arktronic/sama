using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMA.Web.Services;

namespace SAMA.Web.Pages;

public class AboutModel(ApplicationStateService appStateService) : PageModel
{
    public DateTimeOffset StartupTime { get; private set; }

    public TimeSpan Uptime { get; private set; }

    public string Version { get; private set; } = "Unknown";

    public void OnGet()
    {
        StartupTime = appStateService.StartupTime;
        Uptime = DateTimeOffset.UtcNow - StartupTime;
        Version = appStateService.Version;
    }
}
