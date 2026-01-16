using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SAMA.Data;
using SAMA.Data.Entities;
using SAMA.Web.Constants;
using SAMA.Web.Models;

namespace SAMA.Web.Services;

public class WorkspaceAuthorizationService(SamaDbContext _dbContext, UserManager<ApplicationUser> _userManager)
{
    public virtual async Task<bool> CanViewWorkspace(Guid? userId, Guid workspaceId)
    {
        if (await IsGlobalAdmin(userId))
        {
            return true;
        }

        var loggedInUserHasAccess = userId.HasValue && await _dbContext.UserWorkspaces
            .AnyAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId);
        if (loggedInUserHasAccess)
        {
            return true;
        }

        var isPublic = await _dbContext.Workspaces
            .Where(w => w.Id == workspaceId)
            .Select(w => w.IsPublic)
            .FirstOrDefaultAsync();

        return isPublic;
    }

    public virtual async Task<bool> CanEditWorkspace(Guid userId, Guid workspaceId)
    {
        if (await IsGlobalAdmin(userId))
        {
            return true;
        }

        var role = await GetUserWorkspaceRoleInternal(userId, workspaceId);
        return role == AuthConstants.EditorRole;
    }

    public virtual async Task<bool> IsGlobalAdmin(Guid? userId)
    {
        if (!userId.HasValue)
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(user, AuthConstants.AdminRole);
    }

    public virtual async Task<List<Guid>> GetAccessibleWorkspaceIds(Guid? userId)
    {
        if (!userId.HasValue)
        {
            return await _dbContext.Workspaces
                .Where(w => w.IsPublic)
                .Select(w => w.Id)
                .ToListAsync();
        }

        if (await IsGlobalAdmin(userId))
        {
            return await _dbContext.Workspaces
                .Select(w => w.Id)
                .ToListAsync();
        }

        var userWorkspaces = await _dbContext.UserWorkspaces
            .Where(uw => uw.UserId == userId.Value)
            .Select(uw => uw.WorkspaceId)
            .ToListAsync();

        var publicWorkspaces = await _dbContext.Workspaces
            .Where(w => w.IsPublic)
            .Select(w => w.Id)
            .ToListAsync();

        return userWorkspaces.Union(publicWorkspaces).Distinct().ToList();
    }

    public virtual async Task<List<WorkspaceMemberViewModel>> GetWorkspaceMembers(Guid workspaceId)
    {
        return await _dbContext.UserWorkspaces
            .Where(uw => uw.WorkspaceId == workspaceId)
            .OrderBy(uw => uw.User.Email)
            .Select(uw => new WorkspaceMemberViewModel(
                uw.UserId,
                uw.User.Email ?? string.Empty,
                uw.Role,
                uw.Source,
                uw.CreatedAt))
            .ToListAsync();
    }

    private async Task<string?> GetUserWorkspaceRoleInternal(Guid userId, Guid workspaceId)
    {
        return await _dbContext.UserWorkspaces
            .Where(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId)
            .Select(uw => uw.Role)
            .FirstOrDefaultAsync();
    }
}
