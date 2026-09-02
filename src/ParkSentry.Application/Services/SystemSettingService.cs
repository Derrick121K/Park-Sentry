using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.Services;

public class SystemSettingService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public SystemSettingService(IParkSentryDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<SystemSettingDto>> ListAsync(CancellationToken ct = default)
    {
        RequireSuperAdmin();
        return await _db.SystemSettings
            .OrderBy(s => s.Key)
            .Select(s => new SystemSettingDto(s.Id, s.Key, s.Value, s.Description, s.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<SystemSettingDto> UpsertAsync(string key, string value, string? description, CancellationToken ct = default)
    {
        RequireSuperAdmin();
        if (string.IsNullOrWhiteSpace(key))
            throw new ValidationException("Setting key is required.");

        // Never allow secret-like keys to be stored casually via UI without ops review markers.
        if (key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("apikey", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Secret values must be stored in environment/secret manager, not SystemSettings.");

        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting is null)
        {
            setting = new SystemSetting { Key = key.Trim(), Value = value, Description = description };
            _db.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.Description = description;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.OrganizationSettingsChanged, nameof(SystemSetting), setting.Id.ToString(),
            $"System setting upsert: {key}", cancellationToken: ct);
        return new SystemSettingDto(setting.Id, setting.Key, setting.Value, setting.Description, setting.UpdatedAt);
    }

    private void RequireSuperAdmin()
    {
        if (!_tenant.IsSuperAdmin)
            throw new ForbiddenException("Super admin access required.");
    }
}

public record SystemSettingDto(Guid Id, string Key, string Value, string? Description, DateTime UpdatedAt);
