using Microsoft.AspNetCore.SignalR;
using ParkSentry.Application.Interfaces.Notifications;
using ParkSentry.Web.Hubs;

namespace ParkSentry.Web.Services;

public class SignalRParkingNotificationService : IParkingNotificationService
{
    private readonly IHubContext<ParkingHub> _hub;

    public SignalRParkingNotificationService(IHubContext<ParkingHub> hub) => _hub = hub;

    public Task NotifyVehicleEntryAsync(VehicleEntryNotification notification, CancellationToken cancellationToken = default)
        => ParkingHub.NotifyParkingUpdate(_hub, notification.OrganizationId, "entry", notification, cancellationToken);

    public Task NotifyVehicleExitAsync(VehicleExitNotification notification, CancellationToken cancellationToken = default)
    {
        if (notification.IsIdempotentReplay)
            return Task.CompletedTask;

        return ParkingHub.NotifyParkingUpdate(_hub, notification.OrganizationId, "exit", notification, cancellationToken);
    }

    public Task NotifyPaymentAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        if (notification.IsIdempotentReplay)
            return Task.CompletedTask;

        return ParkingHub.NotifyParkingUpdate(_hub, notification.OrganizationId, "payment", notification, cancellationToken);
    }

    public Task NotifyBayUpdateAsync(BayUpdateNotification notification, CancellationToken cancellationToken = default)
        => ParkingHub.NotifyParkingUpdate(_hub, notification.OrganizationId, "bay", notification, cancellationToken);
}
