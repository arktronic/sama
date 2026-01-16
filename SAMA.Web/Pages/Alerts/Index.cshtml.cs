using Microsoft.AspNetCore.Mvc;
using SAMA.Web.Authorization;
using SAMA.Web.Models;
using SAMA.Web.Pages.Shared;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Alerts;

[RequireWorkspaceViewAccess]
public class IndexModel(WorkspaceQueryService _workspaceQueryService, CheckQueryService _checkQueryService, AlertQueryService _alertQueryService)
    : WorkspacePageModel(_workspaceQueryService)
{
    public Guid CheckId { get; set; }

    public string CheckName { get; set; } = string.Empty;

    public IList<AlertListItemViewModel> Alerts { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid? checkId)
    {
        if (!checkId.HasValue)
        {
            return RedirectToPage("/Workspaces/Index");
        }

        var check = await _checkQueryService.GetCheckBasicInfoAsync(checkId.Value);
        if (check == null)
        {
            return NotFound();
        }

        var result = await LoadWorkspaceContextAsync(check.WorkspaceId, "Checks");
        if (result != null)
        {
            return result;
        }

        CheckId = check.Id;
        CheckName = check.Name;

        Alerts = await _alertQueryService.GetAlertsForCheckAsync(checkId.Value);

        return Page();
    }
}
