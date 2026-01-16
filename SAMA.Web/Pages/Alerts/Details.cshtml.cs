using Microsoft.AspNetCore.Mvc;
using SAMA.Web.Authorization;
using SAMA.Web.Models;
using SAMA.Web.Pages.Shared;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Alerts;

[RequireWorkspaceViewAccess]
public class DetailsModel(WorkspaceQueryService _workspaceQueryService, AlertQueryService _alertQueryService)
    : WorkspacePageModel(_workspaceQueryService)
{
    public Guid CheckId { get; set; }

    public string CheckName { get; set; } = string.Empty;

    public AlertDetailsViewModel Alert { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var alert = await _alertQueryService.GetAlertDetailsAsync(id.Value);
        if (alert == null)
        {
            return NotFound();
        }

        var result = await LoadWorkspaceContextAsync(alert.WorkspaceId, "Checks");
        if (result != null)
        {
            return result;
        }

        CheckId = alert.CheckId;
        CheckName = alert.CheckName;
        Alert = alert;

        return Page();
    }
}
