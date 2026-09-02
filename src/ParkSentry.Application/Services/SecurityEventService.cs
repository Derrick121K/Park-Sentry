using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.Services;

public class SecurityEventService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public SecurityEventService(IParkSentryDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<SecurityEventDto>> ListAsync(SecurityEventStatus? status = null, int limit = 100, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var query = _db.SecurityEvents.Where(e => e.OrganizationId == orgId);
        if (status.HasValue)
            query = query.Where(e => e.Status == status);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(Math.Clamp(limit, 1, 500))
            .Select(e => new SecurityEventDto(e.Id, e.SiteId, e.VehicleId, e.EventType, e.Description, e.Severity, e.Status, e.Resolution, e.ResolvedAt, e.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task ResolveAsync(Guid id, string resolution, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var evt = await _db.SecurityEvents.FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == orgId, ct)
            ?? throw new NotFoundException("Security event not found.");
        evt.Status = SecurityEventStatus.Resolved;
        evt.Resolution = resolution;
        evt.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.SecurityEvent, nameof(Domain.Entities.SecurityEvent), evt.Id.ToString(),
            $"Resolved: {resolution}", cancellationToken: ct);
    }

    private Guid RequireOrganizationId()
    {
        if (!_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");
        return _tenant.OrganizationId.Value;
    }
}

public record SecurityEventDto(
    Guid Id, Guid SiteId, Guid? VehicleId, string EventType, string Description,
    SecurityEventSeverity Severity, SecurityEventStatus Status, string? Resolution, DateTime? ResolvedAt, DateTime CreatedAt);
