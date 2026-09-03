using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.DTOs.Parking;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.Services;

public class ParkingBayService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public ParkingBayService(IParkSentryDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<ParkingBayDto>> GetBaysBySiteAsync(Guid siteId, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();

        return await _db.ParkingBays
            .Where(b => b.OrganizationId == orgId && !b.IsDeleted &&
                        b.ParkingZone.ParkingArea.SiteId == siteId)
            .OrderBy(b => b.BayNumber)
            .Select(b => new ParkingBayDto(b.Id, b.BayNumber, b.ParkingZone.Name, b.Type, b.Status, b.IsReserved, b.ParkingZoneId, b.ParkingZone.ParkingAreaId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ParkingAreaDto>> GetParkingStructureAsync(Guid siteId, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();

        var areas = await _db.ParkingAreas
            .Where(a => a.OrganizationId == orgId && a.SiteId == siteId && !a.IsDeleted)
            .Include(a => a.ParkingZones).ThenInclude(z => z.ParkingBays)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

        return areas.Select(a => new ParkingAreaDto(a.Id, a.Name,
            a.ParkingZones.Where(z => !z.IsDeleted).OrderBy(z => z.Name).Select(z => new ParkingZoneDto(z.Id, z.Name, z.Code,
                z.ParkingBays.Where(b => !b.IsDeleted).OrderBy(b => b.BayNumber)
                    .Select(b => new ParkingBayDto(b.Id, b.BayNumber, z.Name, b.Type, b.Status, b.IsReserved, z.Id, a.Id)))))).ToList();
    }

    public async Task<ParkingAreaDto> CreateAreaAsync(CreateParkingAreaRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        await EnsureSiteAsync(request.SiteId, orgId, ct);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Area name is required.");

        var area = new ParkingArea
        {
            OrganizationId = orgId,
            SiteId = request.SiteId,
            Name = request.Name.Trim(),
            IsActive = true
        };
        _db.ParkingAreas.Add(area);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.OrganizationSettingsChanged, nameof(ParkingArea), area.Id.ToString(),
            $"Created area {area.Name}", cancellationToken: ct);
        return new ParkingAreaDto(area.Id, area.Name, []);
    }

    public async Task<ParkingZoneDto> CreateZoneAsync(CreateParkingZoneRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var area = await _db.ParkingAreas.FirstOrDefaultAsync(a => a.Id == request.AreaId && a.OrganizationId == orgId && !a.IsDeleted, ct)
            ?? throw new NotFoundException("Parking area not found.");
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
            throw new ValidationException("Zone name and code are required.");

        var zone = new ParkingZone
        {
            OrganizationId = orgId,
            ParkingAreaId = area.Id,
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            IsActive = true
        };
        _db.ParkingZones.Add(zone);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.OrganizationSettingsChanged, nameof(ParkingZone), zone.Id.ToString(),
            $"Created zone {zone.Name}", cancellationToken: ct);
        return new ParkingZoneDto(zone.Id, zone.Name, zone.Code, []);
    }

    public async Task<ParkingBayDto> CreateBayAsync(CreateParkingBayRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var zone = await _db.ParkingZones
            .Include(z => z.ParkingArea)
            .FirstOrDefaultAsync(z => z.Id == request.ZoneId && z.OrganizationId == orgId && !z.IsDeleted, ct)
            ?? throw new NotFoundException("Parking zone not found.");

        if (string.IsNullOrWhiteSpace(request.BayNumber))
            throw new ValidationException("Bay number is required.");

        var bayNumber = request.BayNumber.Trim().ToUpperInvariant();
        var exists = await _db.ParkingBays.AnyAsync(b => b.ParkingZoneId == zone.Id && b.BayNumber == bayNumber && !b.IsDeleted, ct);
        if (exists)
            throw new ValidationException($"Bay {bayNumber} already exists in this zone.");

        var bay = new ParkingBay
        {
            OrganizationId = orgId,
            ParkingZoneId = zone.Id,
            BayNumber = bayNumber,
            Type = request.Type,
            Status = BayStatus.Available
        };
        _db.ParkingBays.Add(bay);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.BayAssignment, nameof(ParkingBay), bay.Id.ToString(),
            $"Created bay {bay.BayNumber}", cancellationToken: ct);
        return new ParkingBayDto(bay.Id, bay.BayNumber, zone.Name, bay.Type, bay.Status, bay.IsReserved, zone.Id, zone.ParkingAreaId);
    }

    public async Task<ParkingBayDto> UpdateBayAsync(Guid id, UpdateParkingBayRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var bay = await _db.ParkingBays
            .Include(b => b.ParkingZone)
            .FirstOrDefaultAsync(b => b.Id == id && b.OrganizationId == orgId && !b.IsDeleted, ct)
            ?? throw new NotFoundException("Parking bay not found.");

        if (bay.Status == BayStatus.Occupied && request.Status is BayStatus.Available or BayStatus.Maintenance or BayStatus.Reserved)
            throw new ValidationException("Cannot change status of an occupied bay. Close the active session first.");

        bay.BayNumber = request.BayNumber.Trim().ToUpperInvariant();
        bay.Type = request.Type;
        bay.Status = request.Status;
        bay.IsReserved = request.IsReserved;
        bay.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new ParkingBayDto(bay.Id, bay.BayNumber, bay.ParkingZone.Name, bay.Type, bay.Status, bay.IsReserved, bay.ParkingZoneId, null);
    }

    public async Task<int> BulkCreateBaysAsync(BulkCreateBaysRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        await EnsureSiteAsync(request.SiteId, orgId, ct);

        if (request.Count is < 1 or > 500)
            throw new ValidationException("Bay count must be between 1 and 500.");
        if (string.IsNullOrWhiteSpace(request.AreaName) || string.IsNullOrWhiteSpace(request.ZoneName) || string.IsNullOrWhiteSpace(request.ZoneCode))
            throw new ValidationException("Area name, zone name, and zone code are required.");

        var area = await _db.ParkingAreas.FirstOrDefaultAsync(a =>
            a.OrganizationId == orgId && a.SiteId == request.SiteId && a.Name == request.AreaName.Trim() && !a.IsDeleted, ct);
        if (area is null)
        {
            area = new ParkingArea
            {
                OrganizationId = orgId,
                SiteId = request.SiteId,
                Name = request.AreaName.Trim(),
                IsActive = true
            };
            _db.ParkingAreas.Add(area);
            await _db.SaveChangesAsync(ct);
        }

        var zone = await _db.ParkingZones.FirstOrDefaultAsync(z =>
            z.OrganizationId == orgId && z.ParkingAreaId == area.Id && z.Code == request.ZoneCode.Trim().ToUpperInvariant() && !z.IsDeleted, ct);
        if (zone is null)
        {
            zone = new ParkingZone
            {
                OrganizationId = orgId,
                ParkingAreaId = area.Id,
                Name = request.ZoneName.Trim(),
                Code = request.ZoneCode.Trim().ToUpperInvariant(),
                IsActive = true
            };
            _db.ParkingZones.Add(zone);
            await _db.SaveChangesAsync(ct);
        }

        var prefix = (request.BayPrefix ?? "").Trim().ToUpperInvariant();
        var created = 0;
        for (var i = 0; i < request.Count; i++)
        {
            var number = $"{prefix}{request.StartNumber + i}";
            var exists = await _db.ParkingBays.AnyAsync(b => b.ParkingZoneId == zone.Id && b.BayNumber == number && !b.IsDeleted, ct);
            if (exists) continue;

            _db.ParkingBays.Add(new ParkingBay
            {
                OrganizationId = orgId,
                ParkingZoneId = zone.Id,
                BayNumber = number,
                Type = request.Type,
                Status = BayStatus.Available
            });
            created++;
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.BayAssignment, nameof(ParkingBay), zone.Id.ToString(),
            $"Bulk created {created} bays in zone {zone.Code}", cancellationToken: ct);
        return created;
    }

    private async Task EnsureSiteAsync(Guid siteId, Guid orgId, CancellationToken ct)
    {
        var exists = await _db.Sites.AnyAsync(s => s.Id == siteId && s.OrganizationId == orgId && !s.IsDeleted, ct);
        if (!exists)
            throw new NotFoundException("Site not found.");
    }

    private Guid RequireOrganizationId()
    {
        if (!_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");
        return _tenant.OrganizationId.Value;
    }
}
