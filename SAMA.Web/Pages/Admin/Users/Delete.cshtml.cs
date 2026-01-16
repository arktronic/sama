using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMA.Web.Constants;
using SAMA.Web.Extensions;
using SAMA.Web.Models;
using SAMA.Web.Services.Commands;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Admin.Users;

[Authorize(Roles = AuthConstants.AdminRole)]
public class DeleteModel(UserQueryService _userQueryService, UserCommandService _userCommandService)
    : PageModel
{
    public UserViewModel UserToDelete { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        if (User.GetUserId() == id.Value)
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToPage("Index");
        }

        var user = await _userQueryService.GetUserByIdAsync(id.Value);
        if (user == null)
        {
            return NotFound();
        }

        UserToDelete = user;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        if (User.GetUserId() == id.Value)
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToPage("Index");
        }

        var user = await _userQueryService.GetUserByIdAsync(id.Value);
        if (user == null)
        {
            return NotFound();
        }

        try
        {
            await _userCommandService.DeleteUserAsync(
                id.Value,
                User.Identity?.Name ?? "System");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            UserToDelete = user;
            return Page();
        }

        TempData["SuccessMessage"] = $"User {user.Email} deleted successfully.";

        return RedirectToPage("Index");
    }
}
