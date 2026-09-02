using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.DTOs.Vehicles;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;
using ParkSentry.Domain.Helpers;

namespace ParkSentry.Application.Services;

public class VehicleService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public VehicleService(IParkSentryDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<VehicleSearchResult> SearchAsync(string registration, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var normalized = RegistrationNormalizer.Normalize(registration);

        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(v => v.OrganizationId == orgId && v.NormalizedRegistration == normalized && !v.IsDeleted, ct);

        var watchlist = await _db.WatchlistEntries
            .FirstOrDefaultAsync(w => w.OrganizationId == orgId && w.NormalizedRegistration == normalized && w.IsActive, ct);

        VehicleDto? dto = vehicle is null ? null : MapVehicle(vehicle);

        return new VehicleSearchResult(dto, watchlist is not null, watchlist?.Reason, watchlist?.BlockEntry ?? false);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var normalized = RegistrationNormalizer.Normalize(request.RegistrationNumber);

        var existing = await _db.Vehicles
            .AnyAsync(v => v.OrganizationId == orgId && v.NormalizedRegistration == normalized && !v.IsDeleted, ct);
        if (existing)
            throw new ValidationException("Vehicle with this registration already exists.");

        var vehicle = new Vehicle
        {
            OrganizationId = orgId,
            RegistrationNumber = request.RegistrationNumber.Trim().ToUpperInvariant(),
            NormalizedRegistration = normalized,
            VehicleMake = request.VehicleMake,
            VehicleModel = request.VehicleModel,
            VehicleColour = request.VehicleColour,
            VehicleType = request.VehicleType,
            LicenceDiscNumber = request.LicenceDiscNumber
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.VehicleCreated, nameof(Vehicle), vehicle.Id.ToString(), $"Registered {vehicle.RegistrationNumber}", cancellationToken: ct);

        return MapVehicle(vehicle);
    }

    public async Task<IReadOnlyList<VehicleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        return await _db.Vehicles
            .Where(v => v.OrganizationId == orgId && !v.IsDeleted)
            .OrderBy(v => v.RegistrationNumber)
            .Select(v => new VehicleDto(v.Id, v.RegistrationNumber, v.VehicleMake, v.VehicleModel, v.VehicleColour, v.VehicleType))
            .ToListAsync(ct);
    }

    private Guid RequireOrganizationId()
    {
        if (!_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");
        return _tenant.OrganizationId.Value;
    }

    private static VehicleDto MapVehicle(Vehicle v) =>
        new(v.Id, v.RegistrationNumber, v.VehicleMake, v.VehicleModel, v.VehicleColour, v.VehicleType);
}
