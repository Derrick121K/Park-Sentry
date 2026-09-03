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

    public async Task<UserAdminDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("Email and password are required.");
        if (!AppRoles.All.Contains(request.Role) || request.Role == AppRoles.SuperAdmin)
            throw new ValidationException("Invalid role for organization users.");

        var existing = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existing is not null)
            throw new ValidationException("A user with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Email.Trim() : request.DisplayName.Trim(),
            OrganizationId = orgId,
            EmailConfirmed = true,
            IsActive = true
        };

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
            throw new ValidationException(string.Join("; ", create.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, request.Role);
        await _audit.LogAsync(AuditAction.UserCreated, "User", user.Id,
            $"Created user {user.Email} as {request.Role}", cancellationToken: ct);

        return new UserAdminDto(user.Id, user.Email!, user.DisplayName, user.IsActive, [request.Role]);
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
public record CreateUserRequest(string Email, string Password, string DisplayName, string Role);
