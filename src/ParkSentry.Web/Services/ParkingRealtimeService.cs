using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace ParkSentry.Web.Services;

public class ParkingRealtimeService : IAsyncDisposable
{
    private readonly NavigationManager _navigation;
    private HubConnection? _connection;
    private Guid? _joinedOrganizationId;

    public ParkingRealtimeService(NavigationManager navigation) => _navigation = navigation;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public event Func<string, object, Task>? ParkingUpdateReceived;

    public async Task ConnectAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (_connection is not null && _joinedOrganizationId == organizationId && IsConnected)
            return;

        await DisposeAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl(_navigation.ToAbsoluteUri("/hubs/parking"))
            .WithAutomaticReconnect()
            .Build();

        _connection.On<string, object>("ParkingUpdate", async (eventType, data) =>
        {
            if (ParkingUpdateReceived is not null)
                await ParkingUpdateReceived(eventType, data);
        });

        await _connection.StartAsync(cancellationToken);
        await _connection.InvokeAsync("JoinOrganization", organizationId.ToString(), cancellationToken);
        _joinedOrganizationId = organizationId;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
            _joinedOrganizationId = null;
        }
    }
}
