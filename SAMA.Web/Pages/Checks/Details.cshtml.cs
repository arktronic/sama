using Microsoft.AspNetCore.Mvc;
using SAMA.Web.Authorization;
using SAMA.Web.Models;
using SAMA.Web.Pages.Shared;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Checks;

[RequireWorkspaceViewAccess]
public class DetailsModel(WorkspaceQueryService _workspaceQueryService, CheckQueryService _checkQueryService)
    : WorkspacePageModel(_workspaceQueryService)
{
    public CheckDetailsViewModel Check { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var check = await _checkQueryService.GetCheckDetailsAsync(id.Value);
        if (check == null)
        {
            return NotFound();
        }

        var result = await LoadWorkspaceContextAsync(check.WorkspaceId, "Checks");
        if (result != null)
        {
            return result;
        }

        Check = check;

        return Page();
    }

    public async Task<IActionResult> OnGetHistoryAsync(Guid? id, int hours = 24)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var check = await _checkQueryService.GetCheckDetailsAsync(id.Value);
        if (check == null)
        {
            return NotFound();
        }

        var result = await LoadWorkspaceContextAsync(check.WorkspaceId, "Checks");
        if (result != null)
        {
            return result;
        }

        var history = await _checkQueryService.GetCheckHistoryAsync(id.Value, hours);

        return new JsonResult(history);
    }

    public async Task<IActionResult> OnGetUptimeAsync(Guid? id, int hours = 24)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var check = await _checkQueryService.GetCheckDetailsAsync(id.Value);
        if (check == null)
        {
            return NotFound();
        }

        var result = await LoadWorkspaceContextAsync(check.WorkspaceId, "Checks");
        if (result != null)
        {
            return result;
        }

        var uptime = await _checkQueryService.GetCheckUptimeAsync(id.Value, hours);
        if (uptime == null)
        {
            return new JsonResult(new
            {
                UptimePercentage = 0.0,
                TotalChecks = 0,
                UpCount = 0,
                WarnCount = 0,
                DownCount = 0,
                AvailableCount = 0
            });
        }

        return new JsonResult(uptime);
    }
}
