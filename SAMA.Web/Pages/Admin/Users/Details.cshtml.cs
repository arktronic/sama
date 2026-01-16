using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMA.Web.Constants;
using SAMA.Web.Models;
using SAMA.Web.Services.Commands;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Admin.Users;

[Authorize(Roles = AuthConstants.AdminRole)]
public class DetailsModel(
    UserQueryService _userQueryService,
    UserCommandService _userCommandService)
    : PageModel
{
    public UserViewModel UserDetails { get; set; } = new();

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

        UserDetails = user;

        return Page();
    }

    public async Task<IActionResult> OnPostUnlockAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var success = await _userCommandService.UnlockUserAsync(
            id.Value,
            User.Identity?.Name ?? "System");

        if (success)
        {
            TempData["SuccessMessage"] = "User has been unlocked.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to unlock user.";
        }

        return RedirectToPage(new { id });
    }
}
