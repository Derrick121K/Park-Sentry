using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.Services;

public class ReportingService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;

    public ReportingService(IParkSentryDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<OperationalReportDto> GetOperationalReportAsync(DateTime? fromUtc = null, DateTime? toUtc = null, Guid? siteId = null, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var from = fromUtc ?? DateTime.UtcNow.Date;
        var to = toUtc ?? DateTime.UtcNow.Date.AddDays(1);

        if (to <= from)
            throw new ValidationException("Report end must be after start.");

        var sessions = _db.ParkingSessions.Where(s => s.OrganizationId == orgId && s.EntryTime >= from && s.EntryTime < to);
        if (siteId.HasValue)
            sessions = sessions.Where(s => s.SiteId == siteId);

        var activeNow = await _db.ParkingSessions.CountAsync(s =>
            s.OrganizationId == orgId &&
            s.Status == SessionStatus.Active &&
            (!siteId.HasValue || s.SiteId == siteId), ct);

        var entries = await sessions.CountAsync(ct);
        var exits = await sessions.CountAsync(s => s.ExitTime != null && s.ExitTime >= from && s.ExitTime < to, ct);

        var payments = _db.Payments.Where(p =>
            p.OrganizationId == orgId &&
            p.Status == PaymentStatus.Successful &&
            p.CreatedAt >= from && p.CreatedAt < to);

        var revenue = await payments.SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var safetyFees = await _db.ParkingSessions
            .Where(s => s.OrganizationId == orgId && s.ExitTime >= from && s.ExitTime < to && (!siteId.HasValue || s.SiteId == siteId))
            .SumAsync(s => (decimal?)s.SafetyFee, ct) ?? 0m;

        var outstanding = await _db.ParkingSessions
            .Where(s => s.OrganizationId == orgId && s.Status == SessionStatus.Active && (!siteId.HasValue || s.SiteId == siteId))
            .SumAsync(s => (decimal?)(s.ParkingFee + s.SafetyFee - s.DiscountAmount - s.AmountPaid), ct) ?? 0m;

        var methodTotals = await payments
            .GroupBy(p => p.Method)
            .Select(g => new PaymentMethodTotalDto(g.Key.ToString(), g.Sum(x => x.Amount), g.Count()))
            .ToListAsync(ct);

        var securityEvents = await _db.SecurityEvents.CountAsync(e =>
            e.OrganizationId == orgId && e.CreatedAt >= from && e.CreatedAt < to, ct);

        var watchlistHits = await _db.SecurityEvents.CountAsync(e =>
            e.OrganizationId == orgId && e.EventType == "WatchlistMatch" && e.CreatedAt >= from && e.CreatedAt < to, ct);

        var baysQuery = _db.ParkingBays.Where(b => b.OrganizationId == orgId && !b.IsDeleted);
        if (siteId.HasValue)
            baysQuery = baysQuery.Where(b => b.ParkingZone.ParkingArea.SiteId == siteId);

        var totalBays = await baysQuery.CountAsync(ct);
        var occupied = await baysQuery.CountAsync(b => b.Status == BayStatus.Occupied, ct);
        var utilization = totalBays == 0 ? 0 : Math.Round((decimal)occupied / totalBays * 100m, 2);

        return new OperationalReportDto(
            from, to, activeNow, entries, exits, revenue, outstanding, safetyFees,
            methodTotals, securityEvents, watchlistHits, totalBays, occupied, utilization);
    }

    private Guid RequireOrganizationId()
    {
        if (!_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");
        return _tenant.OrganizationId.Value;
    }
}

public record PaymentMethodTotalDto(string Method, decimal Amount, int Count);

public record OperationalReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int ActiveSessions,
    int DailyEntries,
    int DailyExits,
    decimal Revenue,
    decimal OutstandingPayments,
    decimal SafetyFees,
    IReadOnlyList<PaymentMethodTotalDto> PaymentMethodTotals,
    int SecurityEvents,
    int WatchlistHits,
    int TotalBays,
    int OccupiedBays,
    decimal BayUtilizationPercent);
