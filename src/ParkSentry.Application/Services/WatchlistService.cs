using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Common;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;
using ParkSentry.Domain.Helpers;

namespace ParkSentry.Application.Services;

public class WatchlistService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public WatchlistService(IParkSentryDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<IReadOnlyList<WatchlistDto>> ListAsync(CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        return await _db.WatchlistEntries
            .Where(w => w.OrganizationId == orgId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WatchlistDto(w.Id, w.RegistrationNumber, w.Reason, w.BlockEntry, w.ShowWarning, w.IsActive, w.Notes, w.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<WatchlistDto> CreateAsync(CreateWatchlistRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var normalized = RegistrationNormalizer.Normalize(request.RegistrationNumber);
        var entry = new WatchlistEntry
        {
            OrganizationId = orgId,
            RegistrationNumber = normalized,
            NormalizedRegistration = normalized,
            Reason = request.Reason,
            BlockEntry = request.BlockEntry,
            ShowWarning = request.ShowWarning,
            Notes = request.Notes,
            CreatedByUserId = _tenant.UserId ?? string.Empty,
            IsActive = true
        };
        _db.WatchlistEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.WatchlistUpdated, nameof(WatchlistEntry), entry.Id.ToString(),
            $"Watchlist add: {normalized}", cancellationToken: ct);
        return new WatchlistDto(entry.Id, entry.RegistrationNumber, entry.Reason, entry.BlockEntry, entry.ShowWarning, entry.IsActive, entry.Notes, entry.CreatedAt);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var entry = await _db.WatchlistEntries.FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == orgId, ct)
            ?? throw new NotFoundException("Watchlist entry not found.");
        entry.IsActive = false;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.WatchlistUpdated, nameof(WatchlistEntry), entry.Id.ToString(),
            $"Watchlist deactivate: {entry.NormalizedRegistration}", cancellationToken: ct);
    }

    private Guid RequireOrganizationId()
    {
        if (!_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");
        return _tenant.OrganizationId.Value;
    }
}

public record WatchlistDto(Guid Id, string RegistrationNumber, string Reason, bool BlockEntry, bool ShowWarning, bool IsActive, string? Notes, DateTime CreatedAt);
public record CreateWatchlistRequest(string RegistrationNumber, string Reason, bool BlockEntry, bool ShowWarning = true, string? Notes = null);
