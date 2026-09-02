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

        var org = await _db.Organizations.FindAsync([id], ct);
        if (org is null) return null;

        return new OrganizationDetailDto(org.Id, org.Name, org.DisplayName, org.ContactEmail, org.ContactPhone, org.Address, org.Currency, org.Timezone, org.IsActive, org.SafetyFeeEnabled, org.SafetyFeeAmount);
    }

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default)
    {
        if (!_tenant.IsSuperAdmin)
            throw new ForbiddenException("Only platform administrators can create organizations.");

        var org = new Organization
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
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
}
