namespace ParkSentry.Application.Interfaces.Notifications;

public interface IParkingNotificationService
{
    Task NotifyVehicleEntryAsync(VehicleEntryNotification notification, CancellationToken cancellationToken = default);
    Task NotifyVehicleExitAsync(VehicleExitNotification notification, CancellationToken cancellationToken = default);
    Task NotifyPaymentAsync(PaymentNotification notification, CancellationToken cancellationToken = default);
    Task NotifyBayUpdateAsync(BayUpdateNotification notification, CancellationToken cancellationToken = default);
}

public record VehicleEntryNotification(
    Guid OrganizationId,
    Guid SiteId,
    Guid SessionId,
    string RegistrationNumber,
    string? BayNumber,
    DateTime EntryTime);

public record VehicleExitNotification(
    Guid OrganizationId,
    Guid SiteId,
    Guid SessionId,
    string RegistrationNumber,
    string? BayNumber,
    DateTime ExitTime,
    bool IsIdempotentReplay = false);

public record PaymentNotification(
    Guid OrganizationId,
    Guid SessionId,
    decimal Amount,
    string? ReceiptNumber,
    bool IsIdempotentReplay = false);

public record BayUpdateNotification(
    Guid OrganizationId,
    Guid SiteId,
    Guid BayId,
    string BayNumber,
    string Status);
