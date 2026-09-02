using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.DTOs.Parking;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.Services;

public class ParkingBayService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;

    public ParkingBayService(IParkSentryDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<ParkingBayDto>> GetBaysBySiteAsync(Guid siteId, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();

        return await _db.ParkingBays
            .Where(b => b.OrganizationId == orgId && !b.IsDeleted &&
                        b.ParkingZone.ParkingArea.SiteId == siteId)
            .Select(b => new ParkingBayDto(b.Id, b.BayNumber, b.ParkingZone.Name, b.Type, b.Status, b.IsReserved))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ParkingAreaDto>> GetParkingStructureAsync(Guid siteId, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();

        var areas = await _db.ParkingAreas
            .Where(a => a.OrganizationId == orgId && a.SiteId == siteId && !a.IsDeleted)
            .Include(a => a.ParkingZones).ThenInclude(z => z.ParkingBays)
            .ToListAsync(ct);

        return areas.Select(a => new ParkingAreaDto(a.Id, a.Name,
            a.ParkingZones.Where(z => !z.IsDeleted).Select(z => new ParkingZoneDto(z.Id, z.Name, z.Code,
                z.ParkingBays.Where(b => !b.IsDeleted).Select(b => new ParkingBayDto(b.Id, b.BayNumber, z.Name, b.Type, b.Status, b.IsReserved)))))).ToList();
    }

    private Guid RequireOrganizationId()
    {
        if (!_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");
        return _tenant.OrganizationId.Value;
    }
}
