using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAMA.Web.Constants;
using SAMA.Web.Models;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Admin.Users;

[Authorize(Roles = AuthConstants.AdminRole)]
public class IndexModel(UserQueryService _userQueryService)
    : PageModel
{
    public List<UserViewModel> Users { get; set; } = [];

    public async Task OnGetAsync()
    {
        Users = await _userQueryService.GetAllUsersAsync();
    }
}
