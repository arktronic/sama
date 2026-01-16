using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SAMA.Data;
using SAMA.Data.Entities;
using SAMA.Web.Constants;
using SAMA.Web.Models;

namespace SAMA.Web.Services.Commands;

public class UserCommandService(
    UserManager<ApplicationUser> _userManager,
    SamaDbContext _context,
    ILogger<UserCommandService> _logger)
{
    public virtual async Task CreateUserAsync(
        string email,
        string password,
        bool isAdmin,
        string performedBy)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (isAdmin)
        {
            await _userManager.AddToRoleAsync(user, AuthConstants.AdminRole);
        }

        _logger.LogInformation(
            "{PerformedBy} created user {Email} (Admin: {IsAdmin})",
            performedBy,
            email,
            isAdmin);
    }

    public virtual async Task UpdateUserEmailAsync(
        Guid userId,
        string newEmail,
        string performedBy)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        var currentEmail = user.Email;

        user.Email = newEmail;
        user.UserName = newEmail;
        user.NormalizedEmail = _userManager.NormalizeEmail(newEmail);
        user.NormalizedUserName = _userManager.NormalizeName(newEmail);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to update email for user {userId}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _logger.LogInformation(
            "{PerformedBy} changed user email from {OldEmail} to {NewEmail}",
            performedBy,
            currentEmail,
            newEmail);
    }

    public virtual async Task<bool> GrantAdminRoleAsync(Guid userId, string performedBy)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        await _userManager.AddToRoleAsync(user, AuthConstants.AdminRole);

        _logger.LogInformation(
            "{PerformedBy} granted Admin role to user {Email}",
            performedBy,
            user.Email);

        return true;
    }

    public virtual async Task<bool> RevokeAdminRoleAsync(Guid userId, string performedBy)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        await _userManager.RemoveFromRoleAsync(user, AuthConstants.AdminRole);

        _logger.LogInformation(
            "{PerformedBy} revoked Admin role from user {Email}",
            performedBy,
            user.Email);

        return true;
    }

    public virtual async Task<UpdateWorkspaceAssignmentsResult> UpdateWorkspaceAssignmentsAsync(
        Guid userId,
        List<WorkspaceAssignmentViewModel> assignments,
        string performedBy)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return UpdateWorkspaceAssignmentsResult.FailureResult("User not found");
        }

        var existingAssignments = await _context.UserWorkspaces
            .Where(uw => uw.UserId == userId && uw.Source == AuthConstants.ManualSource)
            .ToListAsync();

        var assignedWorkspaceIds = assignments
            .Where(a => a.IsAssigned)
            .Select(a => a.WorkspaceId)
            .ToHashSet();

        var toRemove = existingAssignments
            .Where(ea => !assignedWorkspaceIds.Contains(ea.WorkspaceId))
            .ToList();

        var toAdd = assignments
            .Where(a => a.IsAssigned && !existingAssignments.Any(ea => ea.WorkspaceId == a.WorkspaceId))
            .ToList();

        var toUpdate = assignments
            .Where(a => a.IsAssigned && existingAssignments.Any(ea => ea.WorkspaceId == a.WorkspaceId && ea.Role != a.Role))
            .ToList();

        _context.UserWorkspaces.RemoveRange(toRemove);

        foreach (var assignment in toAdd)
        {
            _context.UserWorkspaces.Add(new UserWorkspace
            {
                UserId = userId,
                WorkspaceId = assignment.WorkspaceId,
                Role = assignment.Role,
                Source = AuthConstants.ManualSource
            });
        }

        foreach (var assignment in toUpdate)
        {
            var existing = existingAssignments.First(ea => ea.WorkspaceId == assignment.WorkspaceId);
            existing.Role = assignment.Role;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "{PerformedBy} updated workspace assignments for user {Email}: {AddCount} added, {RemoveCount} removed, {UpdateCount} updated",
            performedBy,
            user.Email,
            toAdd.Count,
            toRemove.Count,
            toUpdate.Count);

        return UpdateWorkspaceAssignmentsResult.SuccessResult(toAdd.Count, toRemove.Count, toUpdate.Count);
    }

    public virtual async Task DeleteUserAsync(
        Guid userId,
        string performedBy)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        var email = user.Email;

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to delete user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _logger.LogInformation("{PerformedBy} deleted user {Email}", performedBy, email);
    }

    public virtual async Task<bool> UnlockUserAsync(Guid userId, string performedBy)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        _logger.LogInformation("{PerformedBy} unlocked user {Email}", performedBy, user.Email);

        return true;
    }

    public virtual async Task ResetPasswordAsync(
        Guid userId,
        string newPassword,
        string performedBy)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        await _userManager.RemovePasswordAsync(user);
        var result = await _userManager.AddPasswordAsync(user, newPassword);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to reset password for user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _logger.LogInformation(
            "{PerformedBy} reset password for user {Email}",
            performedBy,
            user.Email);
    }
}
