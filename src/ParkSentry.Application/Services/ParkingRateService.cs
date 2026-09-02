using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;
using System.Text.Json;

namespace ParkSentry.Application.Services;

public class ParkingRateService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public ParkingRateService(IParkSentryDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<ParkingRateDto>> ListAsync(CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        return await _db.ParkingRates
            .Where(r => r.OrganizationId == orgId)
            .OrderBy(r => r.Name)
            .Select(r => new ParkingRateDto(r.Id, r.Name, r.SiteId, r.Model, r.GracePeriodMinutes, r.DailyMaximum, r.IsActive, r.TiersJson))
            .ToListAsync(ct);
    }

    public async Task<ParkingRateDto> CreateAsync(UpsertParkingRateRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var rate = new ParkingRate
        {
            OrganizationId = orgId,
            SiteId = request.SiteId,
            Name = request.Name.Trim(),
            Model = request.Model,
            GracePeriodMinutes = request.GracePeriodMinutes,
            DailyMaximum = request.DailyMaximum,
            IsActive = request.IsActive,
            TiersJson = string.IsNullOrWhiteSpace(request.TiersJson) ? "[]" : request.TiersJson
        };
        ValidateTiersJson(rate.TiersJson);
        _db.ParkingRates.Add(rate);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.PricingChanged, nameof(ParkingRate), rate.Id.ToString(),
            $"Rate created: {rate.Name}", cancellationToken: ct);
        return ToDto(rate);
    }

    public async Task<ParkingRateDto> UpdateAsync(Guid id, UpsertParkingRateRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var rate = await _db.ParkingRates.FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == orgId, ct)
            ?? throw new NotFoundException("Parking rate not found.");
        rate.Name = request.Name.Trim();
        rate.SiteId = request.SiteId;
        rate.Model = request.Model;
        rate.GracePeriodMinutes = request.GracePeriodMinutes;
        rate.DailyMaximum = request.DailyMaximum;
        rate.IsActive = request.IsActive;
        rate.TiersJson = string.IsNullOrWhiteSpace(request.TiersJson) ? "[]" : request.TiersJson;
        ValidateTiersJson(rate.TiersJson);
        await _db.SaveChangesAsync(ct);
        return ToDto(rate);
    }

    private static void ValidateTiersJson(string json)
    {
        try { JsonDocument.Parse(json); }
        catch { throw new ValidationException("TiersJson must be valid JSON."); }
    }

    private static ParkingRateDto ToDto(ParkingRate r) =>
        new(r.Id, r.Name, r.SiteId, r.Model, r.GracePeriodMinutes, r.DailyMaximum, r.IsActive, r.TiersJson);

    private Guid RequireOrganizationId()
    {
        if (!_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");
        return _tenant.OrganizationId.Value;
    }
}

public record ParkingRateDto(Guid Id, string Name, Guid? SiteId, PricingModel Model, int? GracePeriodMinutes, decimal? DailyMaximum, bool IsActive, string TiersJson);
public record UpsertParkingRateRequest(string Name, Guid? SiteId, PricingModel Model, int? GracePeriodMinutes, decimal? DailyMaximum, bool IsActive, string TiersJson);
