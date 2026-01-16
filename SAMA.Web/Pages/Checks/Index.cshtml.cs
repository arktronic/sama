using Microsoft.AspNetCore.Mvc;
using SAMA.Web.Authorization;
using SAMA.Web.Models;
using SAMA.Web.Pages.Shared;
using SAMA.Web.Services;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Checks;

[RequireWorkspaceViewAccess]
public class IndexModel(WorkspaceQueryService _workspaceQueryService, CheckQueryService _checkQueryService, GlobalSettingsService _globalSettings)
    : WorkspacePageModel(_workspaceQueryService)
{
    public IList<CheckListItemViewModel> Checks { get; set; } = [];

    public int RefreshIntervalSeconds { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? workspaceId)
    {
        var result = await LoadWorkspaceContextAsync(workspaceId, "Checks");
        if (result != null)
        {
            return result;
        }

        Checks = await _checkQueryService.GetChecksForWorkspaceAsync(WorkspaceId);
        RefreshIntervalSeconds = _globalSettings.DashboardRefreshIntervalSeconds;

        return Page();
    }
}
