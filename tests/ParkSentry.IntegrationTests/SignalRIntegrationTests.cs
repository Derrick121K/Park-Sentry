using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ParkSentry.Application.Interfaces.Notifications;
using ParkSentry.IntegrationTests.Infrastructure;

namespace ParkSentry.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public class SignalRIntegrationTests : IAsyncLifetime
{
    private readonly WebTestFactory _factory;

    public SignalRIntegrationTests(PostgresFixture postgres) => _factory = new WebTestFactory(postgres);

    public Task InitializeAsync() => _factory.InitializeAsync();
    public Task DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task AuthenticatedUser_CanJoinOwnOrganization()
    {
        await using var connection = await CreateHubConnectionAsync(TestIsolationDataSeeder.OrgAGuardEmail);
        await connection.StartAsync();

        var act = async () => await connection.InvokeAsync("JoinOrganization", TestIsolationDataSeeder.OrgAId.ToString());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AuthenticatedUser_CannotJoinOtherOrganization()
    {
        await using var connection = await CreateHubConnectionAsync(TestIsolationDataSeeder.OrgAGuardEmail);
        await connection.StartAsync();

        var act = async () => await connection.InvokeAsync("JoinOrganization", TestIsolationDataSeeder.OrgBId.ToString());
        await act.Should().ThrowAsync<HubException>();
    }

    [Fact]
    public async Task OrgA_Event_DoesNotReach_OrgB_Subscriber()
    {
        var orgAReceived = new TaskCompletionSource<(string EventType, object Data)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var orgBReceived = new TaskCompletionSource<(string EventType, object Data)>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connectionA = await CreateHubConnectionAsync(TestIsolationDataSeeder.OrgAGuardEmail);
        await using var connectionB = await CreateHubConnectionAsync(TestIsolationDataSeeder.OrgBGuardEmail);

        connectionA.On<string, object>("ParkingUpdate", (eventType, data) =>
            orgAReceived.TrySetResult((eventType, data)));
        connectionB.On<string, object>("ParkingUpdate", (eventType, data) =>
            orgBReceived.TrySetResult((eventType, data)));

        await connectionA.StartAsync();
        await connectionB.StartAsync();
        await connectionA.InvokeAsync("JoinOrganization", TestIsolationDataSeeder.OrgAId.ToString());
        await connectionB.InvokeAsync("JoinOrganization", TestIsolationDataSeeder.OrgBId.ToString());

        using var scope = _factory.Services.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<IParkingNotificationService>();
        await notifications.NotifyVehicleEntryAsync(new VehicleEntryNotification(
            TestIsolationDataSeeder.OrgAId,
            TestIsolationDataSeeder.OrgASiteId,
            Guid.NewGuid(),
            "CROSS123",
            "A1",
            DateTime.UtcNow));

        var completed = await Task.WhenAny(orgAReceived.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(orgAReceived.Task);
        var orgAEvent = await orgAReceived.Task;
        orgAEvent.EventType.Should().Be("entry");

        var orgBCompleted = await Task.WhenAny(orgBReceived.Task, Task.Delay(TimeSpan.FromMilliseconds(500)));
        orgBCompleted.Should().NotBe(orgBReceived.Task);
    }

    [Fact]
    public async Task VehicleEntry_ProducesEntryEvent()
    {
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = await CreateHubConnectionAsync(TestIsolationDataSeeder.OrgAGuardEmail);
        connection.On<string, object>("ParkingUpdate", (eventType, _) => received.TrySetResult(eventType));
        await connection.StartAsync();
        await connection.InvokeAsync("JoinOrganization", TestIsolationDataSeeder.OrgAId.ToString());

        using var scope = _factory.Services.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<IParkingNotificationService>();
        await notifications.NotifyVehicleEntryAsync(new VehicleEntryNotification(
            TestIsolationDataSeeder.OrgAId,
            TestIsolationDataSeeder.OrgASiteId,
            Guid.NewGuid(),
            "ENTRY123",
            "A1",
            DateTime.UtcNow));

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(received.Task);
        (await received.Task).Should().Be("entry");
    }

    [Fact]
    public async Task VehicleExit_ProducesExitEvent()
    {
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = await CreateHubConnectionAsync(TestIsolationDataSeeder.OrgAGuardEmail);
        connection.On<string, object>("ParkingUpdate", (eventType, _) => received.TrySetResult(eventType));
        await connection.StartAsync();
        await connection.InvokeAsync("JoinOrganization", TestIsolationDataSeeder.OrgAId.ToString());

        using var scope = _factory.Services.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<IParkingNotificationService>();
        await notifications.NotifyVehicleExitAsync(new VehicleExitNotification(
            TestIsolationDataSeeder.OrgAId,
            TestIsolationDataSeeder.OrgASiteId,
            Guid.NewGuid(),
            "EXIT123",
            "A1",
            DateTime.UtcNow));

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(received.Task);
        (await received.Task).Should().Be("exit");
    }

    [Fact]
    public async Task Payment_ProducesPaymentEvent()
    {
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = await CreateHubConnectionAsync(TestIsolationDataSeeder.OrgAGuardEmail);
        connection.On<string, object>("ParkingUpdate", (eventType, _) => received.TrySetResult(eventType));
        await connection.StartAsync();
        await connection.InvokeAsync("JoinOrganization", TestIsolationDataSeeder.OrgAId.ToString());

        using var scope = _factory.Services.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<IParkingNotificationService>();
        await notifications.NotifyPaymentAsync(new PaymentNotification(
            TestIsolationDataSeeder.OrgAId,
            Guid.NewGuid(),
            25.50m,
            "R-001"));

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(received.Task);
        (await received.Task).Should().Be("payment");
    }

    private async Task<HubConnection> CreateHubConnectionAsync(string email)
    {
        var cookieHandler = new CookieForwardingHandler(_factory.Server.CreateHandler());
        var client = new HttpClient(cookieHandler) { BaseAddress = _factory.Server.BaseAddress };

        using var form = new MultipartFormDataContent
        {
            { new StringContent(email), "email" },
            { new StringContent(TestIsolationDataSeeder.Password), "password" }
        };
        var loginResponse = await client.PostAsync("/account/login", form);
        loginResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);

        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress!, "/hubs/parking"), options =>
            {
                options.HttpMessageHandlerFactory = _ => cookieHandler;
            })
            .WithAutomaticReconnect()
            .Build();
    }

    private sealed class CookieForwardingHandler : DelegatingHandler
    {
        private readonly CookieContainer _cookies = new();

        public CookieForwardingHandler(HttpMessageHandler inner) => InnerHandler = inner;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            var cookieHeader = _cookies.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(cookieHeader))
                request.Headers.Add("Cookie", cookieHeader);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var setCookie in setCookies)
                    _cookies.SetCookies(uri, setCookie);
            }

            return response;
        }
    }
}
