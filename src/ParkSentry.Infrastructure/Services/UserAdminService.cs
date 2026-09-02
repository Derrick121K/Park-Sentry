using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Constants;
using ParkSentry.Domain.Enums;
using ParkSentry.Infrastructure.Identity;

namespace ParkSentry.Infrastructure.Services;

public class UserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public UserAdminService(UserManager<ApplicationUser> userManager, ITenantContext tenant, IAuditService audit)
    {
        _userManager = userManager;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<UserAdminDto>> ListAsync(CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var users = await _userManager.Users
            .Where(u => u.OrganizationId == orgId)
            .OrderBy(u => u.Email)
            .ToListAsync(ct);

        var result = new List<UserAdminDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserAdminDto(user.Id, user.Email!, user.DisplayName, user.IsActive, roles.ToList()));
        }

        return result;
    }

    public async Task SetActiveAsync(string userId, bool isActive, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId, ct)
            ?? throw new NotFoundException("User not found.");
        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);
        await _audit.LogAsync(AuditAction.RoleChanged, "User", user.Id,
            $"User active={isActive}", cancellationToken: ct);
    }

    public async Task SetRoleAsync(string userId, string role, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        if (!AppRoles.All.Contains(role) || role == AppRoles.SuperAdmin)
            throw new ValidationException("Invalid role.");

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId, ct)
            ?? throw new NotFoundException("User not found.");

        var current = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, current);
        await _userManager.AddToRoleAsync(user, role);
        await _audit.LogAsync(AuditAction.RoleChanged, "User", user.Id,
            $"Role changed to {role}", cancellationToken: ct);
    }

    private Guid RequireOrganizationId()
    {
        if (!_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");
        return _tenant.OrganizationId.Value;
    }
}

public record UserAdminDto(string Id, string Email, string DisplayName, bool IsActive, IReadOnlyList<string> Roles);
