using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMA.Web.Constants;
using SAMA.Web.Models;
using SAMA.Web.Services.Commands;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Admin.Users;

[Authorize(Roles = AuthConstants.AdminRole)]
public class EditModel(
    UserQueryService _userQueryService,
    UserCommandService _userCommandService)
    : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }

        public List<WorkspaceAssignmentViewModel> WorkspaceAssignments { get; set; } = [];
    }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var user = await _userQueryService.GetUserByIdAsync(id.Value);
        if (user == null)
        {
            return NotFound();
        }

        var workspaceAssignments = await _userQueryService.GetWorkspacesWithManualAssignmentStatusAsync(id.Value);

        Input = new InputModel
        {
            Id = user.Id,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
            WorkspaceAssignments = workspaceAssignments
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var allWorkspaces = await _userQueryService.GetWorkspacesWithManualAssignmentStatusAsync();

            foreach (var assignment in Input.WorkspaceAssignments)
            {
                var workspace = allWorkspaces.FirstOrDefault(w => w.WorkspaceId == assignment.WorkspaceId);
                if (workspace != null)
                {
                    assignment.WorkspaceName = workspace.WorkspaceName;
                }
            }

            return Page();
        }

        var user = await _userQueryService.GetUserByIdAsync(Input.Id);
        if (user == null)
        {
            return NotFound();
        }

        if (user.Email != Input.Email)
        {
            try
            {
                await _userCommandService.UpdateUserEmailAsync(
                    Input.Id,
                    Input.Email,
                    User.Identity?.Name ?? "System");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                var allWorkspaces = await _userQueryService.GetWorkspacesWithManualAssignmentStatusAsync();

                foreach (var assignment in Input.WorkspaceAssignments)
                {
                    var workspace = allWorkspaces.FirstOrDefault(w => w.WorkspaceId == assignment.WorkspaceId);
                    if (workspace != null)
                    {
                        assignment.WorkspaceName = workspace.WorkspaceName;
                    }
                }

                return Page();
            }
        }

        if (Input.IsAdmin != user.IsAdmin)
        {
            if (Input.IsAdmin)
            {
                await _userCommandService.GrantAdminRoleAsync(Input.Id, User.Identity?.Name ?? "System");
            }
            else
            {
                await _userCommandService.RevokeAdminRoleAsync(Input.Id, User.Identity?.Name ?? "System");
            }
        }

        var assignmentResult = await _userCommandService.UpdateWorkspaceAssignmentsAsync(
            Input.Id,
            Input.WorkspaceAssignments,
            User.Identity?.Name ?? "System");

        if (!assignmentResult.Success)
        {
            ModelState.AddModelError(string.Empty, assignmentResult.ErrorMessage ?? "Failed to update workspace assignments");
            return Page();
        }

        TempData["SuccessMessage"] = $"User {Input.Email} updated successfully.";

        return RedirectToPage("Details", new { id = Input.Id });
    }
}
