using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkSentry.Application.Common;
using ParkSentry.Application.DTOs.Sessions;
using ParkSentry.Application.Interfaces;
using ParkSentry.Application.Interfaces.Notifications;
using ParkSentry.Application.Interfaces.Payments;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;
using ParkSentry.Domain.Helpers;

namespace ParkSentry.Application.Services;

public class ParkingSessionService
{
    private readonly IParkSentryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;
    private readonly IPricingService _pricing;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IBayOccupancyService _bayOccupancy;
    private readonly IParkingNotificationService _notifications;
    private readonly ILogger<ParkingSessionService> _logger;

    public ParkingSessionService(
        IParkSentryDbContext db,
        ITenantContext tenant,
        IAuditService audit,
        IPricingService pricing,
        IPaymentProvider paymentProvider,
        IBayOccupancyService bayOccupancy,
        IParkingNotificationService notifications,
        ILogger<ParkingSessionService> logger)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
        _pricing = pricing;
        _paymentProvider = paymentProvider;
        _bayOccupancy = bayOccupancy;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<VehicleEntryResult> ProcessEntryAsync(VehicleEntryRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var normalized = RegistrationNormalizer.Normalize(request.RegistrationNumber);

        await _db.BeginTransactionAsync(ct);
        try
        {
            var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == request.SiteId && s.OrganizationId == orgId && !s.IsDeleted, ct)
                ?? throw new NotFoundException("Site not found.");

            var watchlist = await _db.WatchlistEntries
                .FirstOrDefaultAsync(w => w.OrganizationId == orgId && w.NormalizedRegistration == normalized && w.IsActive, ct);

            if (watchlist?.BlockEntry == true)
                throw new ValidationException($"Vehicle {request.RegistrationNumber} is blocked from entry: {watchlist.Reason}");

            var vehicle = await _db.Vehicles
                .FirstOrDefaultAsync(v => v.OrganizationId == orgId && v.NormalizedRegistration == normalized && !v.IsDeleted, ct);

            if (vehicle is null)
            {
                vehicle = new Vehicle
                {
                    OrganizationId = orgId,
                    RegistrationNumber = normalized,
                    NormalizedRegistration = normalized,
                    VehicleMake = request.VehicleMake,
                    VehicleModel = request.VehicleModel,
                    VehicleColour = request.VehicleColour
                };
                _db.Vehicles.Add(vehicle);
                await _db.SaveChangesAsync(ct);
            }

            var activeSession = await _db.ParkingSessions
                .AnyAsync(s => s.VehicleId == vehicle.Id && s.Status == SessionStatus.Active, ct);
            if (activeSession)
                throw new ValidationException("Vehicle already has an active parking session.");

            ParkingBay? bay = null;
            if (request.ParkingBayId.HasValue)
            {
                if (!await _bayOccupancy.TryOccupyBayAsync(request.ParkingBayId.Value, orgId, ct))
                    throw new ValidationException("Parking bay is no longer available.");

                bay = await _db.ParkingBays
                    .FirstOrDefaultAsync(b => b.Id == request.ParkingBayId && b.OrganizationId == orgId, ct);
            }

            var session = new ParkingSession
            {
                OrganizationId = orgId,
                SiteId = site.Id,
                VehicleId = vehicle.Id,
                ParkingBayId = bay?.Id,
                Status = SessionStatus.Active,
                EntryTime = DateTime.UtcNow,
                EntryUserId = _tenant.UserId,
                Notes = request.Notes
            };

            _db.ParkingSessions.Add(session);
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync(AuditAction.VehicleEntry, nameof(ParkingSession), session.Id.ToString(), $"Entry: {vehicle.RegistrationNumber}", cancellationToken: ct);

            if (bay is not null)
                await _audit.LogAsync(AuditAction.BayAssignment, nameof(ParkingBay), bay.Id.ToString(), $"Assigned to {vehicle.RegistrationNumber}", cancellationToken: ct);

            if (watchlist?.ShowWarning == true)
            {
                _db.SecurityEvents.Add(new SecurityEvent
                {
                    OrganizationId = orgId,
                    SiteId = site.Id,
                    VehicleId = vehicle.Id,
                    UserId = _tenant.UserId,
                    EventType = "WatchlistMatch",
                    Description = $"Watchlisted vehicle entered: {watchlist.Reason}",
                    Severity = SecurityEventSeverity.Medium
                });
                await _db.SaveChangesAsync(ct);
                await _audit.LogAsync(AuditAction.SecurityEvent, nameof(SecurityEvent), vehicle.Id.ToString(), watchlist.Reason, cancellationToken: ct);
            }

            await _db.CommitTransactionAsync(ct);
            _logger.LogInformation("Vehicle entry recorded for {Registration} session {SessionId}", vehicle.RegistrationNumber, session.Id);

            await _notifications.NotifyVehicleEntryAsync(new VehicleEntryNotification(
                orgId, site.Id, session.Id, vehicle.RegistrationNumber, bay?.BayNumber, session.EntryTime), ct);

            if (bay is not null)
            {
                await _notifications.NotifyBayUpdateAsync(new BayUpdateNotification(
                    orgId, site.Id, bay.Id, bay.BayNumber, BayStatus.Occupied.ToString()), ct);
            }

            return new VehicleEntryResult(session.Id, vehicle.Id, vehicle.RegistrationNumber, bay?.Id, bay?.BayNumber,
                watchlist is not null, watchlist?.Reason);
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueConstraintViolation(ex))
        {
            await _db.RollbackTransactionAsync(ct);
            _logger.LogWarning("Concurrent entry rejected for registration {Registration}", normalized);
            throw new ValidationException("Vehicle already has an active parking session.");
        }
        catch
        {
            await _db.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<VehicleExitResult> ProcessExitAsync(VehicleExitRequest request, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await FindIdempotentExitResultAsync(orgId, request.IdempotencyKey, ct);
                if (existing is not null)
                    return existing;
            }

        decimal outstanding;
        decimal parkingFee;
        decimal safetyFee;
        decimal total;
        Guid sessionId;
        string currency;

        await _db.BeginTransactionAsync(ct);
        try
        {
            var session = await _db.ParkingSessions
                .Include(s => s.Vehicle)
                .Include(s => s.Organization)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.OrganizationId == orgId && s.Status == SessionStatus.Active, ct)
                ?? throw new NotFoundException("Active parking session not found.");

            var exitTime = DateTime.UtcNow;
            var rate = await _db.ParkingRates
                .Where(r => r.OrganizationId == orgId && r.IsActive && (r.SiteId == null || r.SiteId == session.SiteId))
                .OrderByDescending(r => r.SiteId)
                .FirstOrDefaultAsync(ct)
                ?? throw new ValidationException("No active parking rate configured.");

            parkingFee = await _pricing.CalculateParkingFeeAsync(rate, session.EntryTime, exitTime, ct);
            safetyFee = _pricing.CalculateSafetyFee(session.Organization, parkingFee);
            total = parkingFee + safetyFee - session.DiscountAmount;
            outstanding = total - session.AmountPaid;
            sessionId = session.Id;
            currency = session.Organization.Currency;

            if (outstanding > 0 && session.Organization.ExitPolicy == ExitPolicy.BlockUntilPaid && !request.ProcessPayment)
                throw new ValidationException($"Outstanding balance of {outstanding:C} must be paid before exit.");

            await _db.CommitTransactionAsync(ct);
        }
        catch
        {
            await _db.RollbackTransactionAsync(ct);
            throw;
        }

        PaymentResult? paymentResult = null;
        if (outstanding > 0 && request.ProcessPayment)
        {
            _logger.LogInformation("Processing exit payment for session {SessionId} amount {Amount}", sessionId, outstanding);
            paymentResult = await _paymentProvider.ProcessPaymentAsync(
                new PaymentRequest(sessionId, outstanding, currency, "Parking exit payment"), ct);

            if (!paymentResult.Success)
            {
                _logger.LogWarning("Payment failed for session {SessionId}: {Error}", sessionId, paymentResult.ErrorMessage);
                throw new ValidationException($"Payment failed: {paymentResult.ErrorMessage}");
            }
        }

        await _db.BeginTransactionAsync(ct);
        try
        {
            var session = await _db.ParkingSessions
                .Include(s => s.Vehicle)
                .Include(s => s.ParkingBay)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.OrganizationId == orgId && s.Status == SessionStatus.Active, ct)
                ?? throw new NotFoundException("Active parking session not found.");

            if (paymentResult?.Success == true)
            {
                var payment = new Payment
                {
                    OrganizationId = orgId,
                    ParkingSessionId = session.Id,
                    Amount = outstanding,
                    Currency = currency,
                    Status = PaymentStatus.Successful,
                    Method = PaymentMethod.Mock,
                    Provider = paymentResult.Provider,
                    ProviderTransactionId = paymentResult.TransactionId,
                    IdempotencyKey = request.IdempotencyKey,
                    CompletedAt = DateTime.UtcNow,
                    Items =
                    [
                        new PaymentItem { Description = "Parking fee", Amount = parkingFee },
                        new PaymentItem { Description = "Safety fee", Amount = safetyFee }
                    ]
                };
                _db.Payments.Add(payment);
                session.AmountPaid += outstanding;
                await _audit.LogAsync(AuditAction.Payment, nameof(Payment), payment.Id.ToString(), $"Payment processed for session {session.Id}", cancellationToken: ct);
            }

            session.ParkingFee = parkingFee;
            session.SafetyFee = safetyFee;
            session.ExitTime = DateTime.UtcNow;
            session.ExitUserId = _tenant.UserId;
            session.Status = SessionStatus.Completed;

            if (session.ParkingBay is not null)
                session.ParkingBay.Status = BayStatus.Available;

            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync(AuditAction.VehicleExit, nameof(ParkingSession), session.Id.ToString(), $"Exit: {session.Vehicle.RegistrationNumber}", cancellationToken: ct);
            await _db.CommitTransactionAsync(ct);

            _logger.LogInformation("Vehicle exit completed for session {SessionId}", session.Id);

            await _notifications.NotifyVehicleExitAsync(new VehicleExitNotification(
                orgId, session.SiteId, session.Id, session.Vehicle.RegistrationNumber,
                session.ParkingBay?.BayNumber, session.ExitTime!.Value), ct);

            if (paymentResult?.Success == true)
            {
                await _notifications.NotifyPaymentAsync(new PaymentNotification(
                    orgId, session.Id, outstanding, paymentResult.TransactionId), ct);
            }

            if (session.ParkingBay is not null)
            {
                await _notifications.NotifyBayUpdateAsync(new BayUpdateNotification(
                    orgId, session.SiteId, session.ParkingBay.Id, session.ParkingBay.BayNumber,
                    BayStatus.Available.ToString()), ct);
            }

            return new VehicleExitResult(session.Id, parkingFee, safetyFee, total, session.AmountPaid,
                total - session.AmountPaid, paymentResult?.Success == true, paymentResult?.TransactionId);
        }
        catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueConstraintViolation(ex))
        {
            await _db.RollbackTransactionAsync(ct);
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await FindIdempotentExitResultAsync(orgId, request.IdempotencyKey, ct);
                if (existing is not null)
                    return existing;
            }
            throw;
        }
        catch
        {
            await _db.RollbackTransactionAsync(ct);
            throw;
        }
    }

    private async Task<VehicleExitResult?> FindIdempotentExitResultAsync(Guid orgId, string idempotencyKey, CancellationToken ct)
    {
        var existingPayment = await _db.Payments
            .Include(p => p.ParkingSession)
            .FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.IdempotencyKey == idempotencyKey, ct);

        if (existingPayment?.ParkingSession is null)
            return null;

        var session = existingPayment.ParkingSession;
        var total = session.ParkingFee + session.SafetyFee - session.DiscountAmount;
        return new VehicleExitResult(
            session.Id,
            session.ParkingFee,
            session.SafetyFee,
            total,
            session.AmountPaid,
            total - session.AmountPaid,
            existingPayment.Status == PaymentStatus.Successful,
            existingPayment.ProviderTransactionId);
    }

    public async Task<IReadOnlyList<ParkingSessionDto>> GetActiveSessionsAsync(Guid? siteId = null, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();

        var query = _db.ParkingSessions
            .Where(s => s.OrganizationId == orgId && s.Status == SessionStatus.Active)
            .Include(s => s.Vehicle)
            .Include(s => s.Site)
            .Include(s => s.ParkingBay)
            .AsQueryable();

        if (siteId.HasValue)
            query = query.Where(s => s.SiteId == siteId);

        var sessions = await query.OrderByDescending(s => s.EntryTime).ToListAsync(ct);

        return sessions.Select(s => new ParkingSessionDto(
            s.Id, s.Vehicle.RegistrationNumber, s.Site.Name, s.ParkingBay?.BayNumber,
            s.Status, s.EntryTime, s.ExitTime, s.ParkingFee, s.SafetyFee, s.AmountPaid,
            s.ParkingFee + s.SafetyFee - s.DiscountAmount - s.AmountPaid)).ToList();
    }

    public async Task<IReadOnlyList<SessionSummaryDto>> GetHistoryAsync(int limit = 50, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();

        return await _db.ParkingSessions
            .Where(s => s.OrganizationId == orgId)
            .Include(s => s.Vehicle)
            .Include(s => s.Site)
            .OrderByDescending(s => s.EntryTime)
            .Take(limit)
            .Select(s => new SessionSummaryDto(s.Id, s.Vehicle.RegistrationNumber, s.EntryTime, s.ExitTime, s.Status, s.Site.Name))
            .ToListAsync(ct);
    }

    public async Task<ParkingSessionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var orgId = RequireOrganizationId();
        var s = await _db.ParkingSessions
            .Include(x => x.Vehicle).Include(x => x.Site).Include(x => x.ParkingBay)
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId, ct);

        if (s is null) return null;

        return new ParkingSessionDto(s.Id, s.Vehicle.RegistrationNumber, s.Site.Name, s.ParkingBay?.BayNumber,
            s.Status, s.EntryTime, s.ExitTime, s.ParkingFee, s.SafetyFee, s.AmountPaid,
            s.ParkingFee + s.SafetyFee - s.DiscountAmount - s.AmountPaid);
    }

    private Guid RequireOrganizationId()
    {
        if (_tenant.IsSuperAdmin && !_tenant.OrganizationId.HasValue)
            throw new ForbiddenException("Organization context required.");

        if (!_tenant.OrganizationId.HasValue)
        {
            _logger.LogWarning("Tenant resolution failed for user {UserId}", _tenant.UserId);
            throw new ForbiddenException("Organization context required.");
        }

        return _tenant.OrganizationId.Value;
    }
}
