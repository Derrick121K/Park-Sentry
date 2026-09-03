using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.DTOs.Sites;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.Services;

public class SiteService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public SiteService(IParkSentryDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<SiteDto>> GetSitesAsync(CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();

        var sites = await _db.Sites
            .Where(s => s.OrganizationId == orgId && !s.IsDeleted)
            .Include(s => s.ParkingAreas).ThenInclude(a => a.ParkingZones).ThenInclude(z => z.ParkingBays)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return sites.Select(MapSite).ToList();
    }

    public async Task<SiteDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var site = await _db.Sites
            .Include(s => s.ParkingAreas).ThenInclude(a => a.ParkingZones).ThenInclude(z => z.ParkingBays)
            .FirstOrDefaultAsync(s => s.Id == id && s.OrganizationId == orgId && !s.IsDeleted, ct);

        return site is null ? null : MapSite(site);
    }

    public async Task<SiteDto> CreateAsync(CreateSiteRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Site name is required.");

        var site = new Site
        {
            OrganizationId = orgId,
            Name = request.Name.Trim(),
            Description = request.Description,
            Address = request.Address,
            IsActive = true
        };

        _db.Sites.Add(site);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.SiteCreated, nameof(Site), site.Id.ToString(), $"Created site {site.Name}", cancellationToken: ct);

        return new SiteDto(site.Id, site.OrganizationId, site.Name, site.Address, site.IsActive, 0, 0, 0, site.Description);
    }

    public async Task<SiteDto> UpdateAsync(Guid id, UpdateSiteRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var site = await _db.Sites
            .Include(s => s.ParkingAreas).ThenInclude(a => a.ParkingZones).ThenInclude(z => z.ParkingBays)
            .FirstOrDefaultAsync(s => s.Id == id && s.OrganizationId == orgId && !s.IsDeleted, ct)
            ?? throw new NotFoundException("Site not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Site name is required.");

        site.Name = request.Name.Trim();
        site.Description = request.Description;
        site.Address = request.Address;
        site.IsActive = request.IsActive;
        site.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.OrganizationSettingsChanged, nameof(Site), site.Id.ToString(),
            $"Updated site {site.Name} active={site.IsActive}", cancellationToken: ct);

        return MapSite(site);
    }

    private Guid RequireOrganizationId()
    {
        if (_tenant.IsSuperAdmin && !_tenant.OrganizationId.HasValue)
            throw new ValidationException("Organization context required.");
        if (!_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");
        return _tenant.OrganizationId.Value;
    }

    private static SiteDto MapSite(Site site)
    {
        var bays = site.ParkingAreas.SelectMany(a => a.ParkingZones).SelectMany(z => z.ParkingBays).Where(b => !b.IsDeleted).ToList();
        return new SiteDto(
            site.Id, site.OrganizationId, site.Name, site.Address, site.IsActive,
            bays.Count,
            bays.Count(b => b.Status == BayStatus.Available),
            bays.Count(b => b.Status == BayStatus.Occupied),
            site.Description);
    }
}
