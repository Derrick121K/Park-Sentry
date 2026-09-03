using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.DTOs.Organizations;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.Services;

public class OrganizationService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public OrganizationService(IParkSentryDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<OrganizationDto>> GetAllAsync(CancellationToken ct = default)
    {
        if (!_tenant.IsSuperAdmin)
            throw new ForbiddenException("Only platform administrators can list all organizations.");

        return await _db.Organizations
            .OrderBy(o => o.Name)
            .Select(o => new OrganizationDto(o.Id, o.Name, o.DisplayName, o.Currency, o.Timezone, o.IsActive))
            .ToListAsync(ct);
    }

    public async Task<OrganizationDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.IsSuperAdmin && _tenant.OrganizationId != id)
            throw new ForbiddenException("Access denied.");

        var org = await _db.Organizations
            .Include(o => o.Branding)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        if (org is null) return null;

        return MapDetail(org);
    }

    public async Task<OrganizationDetailDto?> GetCurrentAsync(CancellationToken ct = default)
    {
        if (!_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");
        return await GetByIdAsync(_tenant.OrganizationId.Value, ct);
    }

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default)
    {
        if (!_tenant.IsSuperAdmin)
            throw new ForbiddenException("Only platform administrators can create organizations.");

        var org = new Organization
        {
            Name = request.Name.Trim(),
            DisplayName = request.DisplayName.Trim(),
            ContactEmail = request.ContactEmail,
            Currency = request.Currency,
            Timezone = request.Timezone,
            Branding = new OrganizationBranding()
        };

        _db.Organizations.Add(org);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.OrganizationCreated, nameof(Organization), org.Id.ToString(), $"Created organization {org.Name}", cancellationToken: ct);

        return new OrganizationDto(org.Id, org.Name, org.DisplayName, org.Currency, org.Timezone, org.IsActive);
    }

    public async Task<OrganizationDetailDto> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default)
    {
        if (!_tenant.IsSuperAdmin && _tenant.OrganizationId != id)
            throw new ForbiddenException("Access denied.");

        var org = await _db.Organizations.Include(o => o.Branding).FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Organization not found.");

        org.DisplayName = request.DisplayName.Trim();
        org.ContactEmail = request.ContactEmail;
        org.ContactPhone = request.ContactPhone;
        org.Address = request.Address;
        org.IsActive = request.IsActive;
        org.SafetyFeeEnabled = request.SafetyFeeEnabled;
        org.SafetyFeeAmount = request.SafetyFeeAmount;
        if (!string.IsNullOrWhiteSpace(request.Currency))
            org.Currency = request.Currency;
        if (!string.IsNullOrWhiteSpace(request.Timezone))
            org.Timezone = request.Timezone;
        org.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.OrganizationSettingsChanged, nameof(Organization), org.Id.ToString(),
            $"Updated organization {org.DisplayName}", cancellationToken: ct);
        return MapDetail(org);
    }

    public async Task<OrganizationDetailDto> UpdateBrandingAsync(Guid id, UpdateBrandingRequest request, CancellationToken ct = default)
    {
        if (!_tenant.IsSuperAdmin && _tenant.OrganizationId != id)
            throw new ForbiddenException("Access denied.");

        var org = await _db.Organizations.Include(o => o.Branding).FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Organization not found.");

        org.Branding ??= new OrganizationBranding { OrganizationId = org.Id };
        org.Branding.LogoUrl = request.LogoUrl;
        org.Branding.PrimaryColor = string.IsNullOrWhiteSpace(request.PrimaryColor) ? "#1B2A4A" : request.PrimaryColor;
        org.Branding.SecondaryColor = string.IsNullOrWhiteSpace(request.SecondaryColor) ? "#00A896" : request.SecondaryColor;
        org.Branding.AccentColor = request.AccentColor;
        org.Branding.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.OrganizationSettingsChanged, nameof(OrganizationBranding), org.Id.ToString(),
            "Updated branding", cancellationToken: ct);
        return MapDetail(org);
    }

    private static OrganizationDetailDto MapDetail(Organization org) =>
        new(org.Id, org.Name, org.DisplayName, org.ContactEmail, org.ContactPhone, org.Address, org.Currency, org.Timezone,
            org.IsActive, org.SafetyFeeEnabled, org.SafetyFeeAmount,
            org.Branding?.LogoUrl, org.Branding?.PrimaryColor ?? "#1B2A4A", org.Branding?.SecondaryColor ?? "#00A896", org.Branding?.AccentColor);
}
