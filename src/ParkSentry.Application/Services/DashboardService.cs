using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.DTOs.Dashboard;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.Services;

public class DashboardService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;

    public DashboardService(IParkSentryDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(Guid? siteId = null, CancellationToken ct = default)
    {
        if (!_tenant.OrganizationId.HasValue)
            return new DashboardStatsDto(0, 0, 0, 0, 0, 0, 0, 0, 0);

        var orgId = _tenant.OrganizationId.Value;
        var today = DateTime.UtcNow.Date;

        var baysQuery = _db.ParkingBays.Where(b => b.OrganizationId == orgId && !b.IsDeleted);
        if (siteId.HasValue)
            baysQuery = baysQuery.Where(b => b.ParkingZone.ParkingArea.SiteId == siteId);

        var bays = await baysQuery.ToListAsync(ct);

        var sessionsQuery = _db.ParkingSessions.Where(s => s.OrganizationId == orgId);
        if (siteId.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.SiteId == siteId);

        var todaySessions = await sessionsQuery.Where(s => s.EntryTime >= today).ToListAsync(ct);
        var activeCount = await sessionsQuery.CountAsync(s => s.Status == SessionStatus.Active, ct);

        var todayRevenue = await _db.Payments
            .Where(p => p.OrganizationId == orgId && p.Status == PaymentStatus.Successful && p.CreatedAt >= today)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0;

        return new DashboardStatsDto(
            bays.Count,
            bays.Count(b => b.Status == BayStatus.Available),
            bays.Count(b => b.Status == BayStatus.Occupied),
            bays.Count(b => b.Status == BayStatus.Reserved),
            bays.Count(b => b.Status == BayStatus.Maintenance),
            activeCount,
            todaySessions.Count,
            todaySessions.Count(s => s.ExitTime >= today),
            todayRevenue,
            await _db.SecurityEvents.CountAsync(e => e.OrganizationId == orgId && e.Status == SecurityEventStatus.Open, ct),
            await _db.WatchlistEntries.CountAsync(w => w.OrganizationId == orgId && w.IsActive, ct));
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(int limit = 50, CancellationToken ct = default)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!_tenant.IsSuperAdmin)
        {
            if (!_tenant.OrganizationId.HasValue)
                return [];
            query = query.Where(a => a.OrganizationId == _tenant.OrganizationId);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new AuditLogDto(a.Id, a.UserId, a.Action, a.EntityType, a.EntityId, a.Details, a.CreatedAt))
            .ToListAsync(ct);
    }
}
