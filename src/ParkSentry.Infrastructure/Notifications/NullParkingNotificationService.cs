using ParkSentry.Application.Interfaces.Notifications;

namespace ParkSentry.Infrastructure.Notifications;

public class NullParkingNotificationService : IParkingNotificationService
{
    public Task NotifyVehicleEntryAsync(VehicleEntryNotification notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task NotifyVehicleExitAsync(VehicleExitNotification notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task NotifyPaymentAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task NotifyBayUpdateAsync(BayUpdateNotification notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
