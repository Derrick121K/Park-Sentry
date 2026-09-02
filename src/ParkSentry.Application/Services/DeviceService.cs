using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.Services;

public class DeviceService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public DeviceService(IParkSentryDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<DeviceDto>> ListDevicesAsync(CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        return await _db.Devices
            .Where(d => d.OrganizationId == orgId)
            .OrderBy(d => d.Name)
            .Select(d => new DeviceDto(d.Id, d.Name, d.DeviceType, d.Identifier, d.IsActive, d.LastSeenAt, d.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<DeviceDto> RegisterAsync(RegisterDeviceRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var device = new Device
        {
            OrganizationId = orgId,
            Name = request.Name.Trim(),
            DeviceType = request.DeviceType,
            Identifier = request.Identifier,
            IsActive = true,
            LastSeenAt = DateTime.UtcNow
        };
        _db.Devices.Add(device);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.DeviceRegistered, nameof(Device), device.Id.ToString(),
            $"Device registered: {device.Name}", cancellationToken: ct);
        return new DeviceDto(device.Id, device.Name, device.DeviceType, device.Identifier, device.IsActive, device.LastSeenAt, device.CreatedAt);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == orgId, ct)
            ?? throw new NotFoundException("Device not found.");
        device.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ScannerConfigDto>> ListScannersAsync(CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        return await _db.ScannerConfigurations
            .Where(s => s.OrganizationId == orgId)
            .OrderBy(s => s.ProviderName)
            .Select(s => new ScannerConfigDto(s.Id, s.ProviderName, s.IsActive, s.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<ScannerConfigDto> UpsertScannerAsync(string providerName, bool isActive, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        // Never expose SettingsJson secrets to clients — store empty/safe config only via ops tooling.
        var existing = await _db.ScannerConfigurations
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId && s.ProviderName == providerName, ct);
        if (existing is null)
        {
            existing = new ScannerConfiguration
            {
                OrganizationId = orgId,
                ProviderName = providerName,
                IsActive = isActive
            };
            _db.ScannerConfigurations.Add(existing);
        }
        else
        {
            existing.IsActive = isActive;
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.ScannerConfigurationChanged, nameof(ScannerConfiguration), existing.Id.ToString(),
            $"Scanner config: {providerName} active={isActive}", cancellationToken: ct);
        return new ScannerConfigDto(existing.Id, existing.ProviderName, existing.IsActive, existing.CreatedAt);
    }

    private Guid RequireOrganizationId()
    {
        if (!_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");
        return _tenant.OrganizationId.Value;
    }
}

public record DeviceDto(Guid Id, string Name, string? DeviceType, string? Identifier, bool IsActive, DateTime? LastSeenAt, DateTime CreatedAt);
public record RegisterDeviceRequest(string Name, string? DeviceType, string? Identifier);
public record ScannerConfigDto(Guid Id, string ProviderName, bool IsActive, DateTime CreatedAt);
