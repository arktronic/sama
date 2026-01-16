using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMA.Web.Constants;
using SAMA.Web.Models;
using SAMA.Web.Services.Commands;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Workspaces;

[Authorize(Roles = AuthConstants.AdminRole)]
public class DeleteModel(WorkspaceQueryService _workspaceQueryService, WorkspaceCommandService _workspaceCommandService)
    : PageModel
{
    public WorkspaceDetailsViewModel WorkspaceToDelete { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var workspace = await _workspaceQueryService.GetWorkspaceDetailsAsync(id.Value);
        if (workspace == null)
        {
            return NotFound();
        }

        WorkspaceToDelete = workspace;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var workspace = await _workspaceQueryService.GetWorkspaceByIdAsync(id.Value);
        if (workspace == null)
        {
            return NotFound();
        }

        var workspaceName = workspace.Name;
        var success = await _workspaceCommandService.DeleteWorkspaceAsync(
            id.Value,
            User.Identity?.Name ?? "System");

        if (!success)
        {
            return BadRequest();
        }

        TempData["SuccessMessage"] = $"Workspace '{workspaceName}' deleted successfully.";

        return RedirectToPage("Index");
    }
}
