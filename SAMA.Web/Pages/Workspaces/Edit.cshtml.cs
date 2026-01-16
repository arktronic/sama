using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMA.Web.Constants;
using SAMA.Web.Services.Commands;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Workspaces;

[Authorize(Roles = AuthConstants.AdminRole)]
public class EditModel(WorkspaceQueryService _workspaceQueryService, WorkspaceCommandService _workspaceCommandService)
    : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Workspace name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public bool IsPublic { get; set; }
    }

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

        Input = new InputModel
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            IsPublic = workspace.IsPublic
        };

        // Set ViewData for workspace layout
        ViewData["WorkspaceId"] = workspace.Id.ToString();
        ViewData["WorkspaceName"] = workspace.Name;
        ViewData["ActiveTab"] = "Settings";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // Reload workspace info for layout on validation failure
            var ws = await _workspaceQueryService.GetWorkspaceDetailsAsync(Input.Id);
            if (ws != null)
            {
                ViewData["WorkspaceId"] = ws.Id.ToString();
                ViewData["WorkspaceName"] = ws.Name;
                ViewData["ActiveTab"] = "Settings";
            }

            return Page();
        }

        var success = await _workspaceCommandService.UpdateWorkspaceAsync(
            Input.Id,
            Input.Name,
            Input.Description,
            Input.IsPublic,
            User.Identity?.Name ?? "System");

        if (!success)
        {
            return BadRequest();
        }

        TempData["SuccessMessage"] = $"Workspace '{Input.Name}' updated successfully.";

        return RedirectToPage("/Dashboard/Index", new { workspaceId = Input.Id });
    }
}
