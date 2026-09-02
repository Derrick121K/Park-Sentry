using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ParkSentry.Application.DTOs.Auth;
using ParkSentry.Application.DTOs.Sessions;
using ParkSentry.Application.DTOs.Vehicles;
using ParkSentry.Domain.Constants;
using ParkSentry.Infrastructure.Identity;
using ParkSentry.IntegrationTests.Infrastructure;

namespace ParkSentry.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public class ParkSentryApiTests : IAsyncLifetime
{
    private readonly ApiTestFactory _factory;

    public ParkSentryApiTests(PostgresFixture postgres) => _factory = new ApiTestFactory(postgres);

    public Task InitializeAsync() => _factory.InitializeAsync();
    public Task DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(TestIsolationDataSeeder.OrgAGuardEmail, TestIsolationDataSeeder.Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions());
        result.Should().NotBeNull();
        result!.Token.Should().NotBeEmpty();
        result.Roles.Should().Contain("SECURITY_GUARD");
        result.OrganizationId.Should().Be(TestIsolationDataSeeder.OrgAId);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(TestIsolationDataSeeder.OrgAGuardEmail, "WrongPassword1!"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VehicleEntryAndExit_EndToEnd()
    {
        var client = await CreateAuthenticatedClient(TestIsolationDataSeeder.OrgAAdminEmail);

        var reg = $"TST{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        var entryResponse = await client.PostAsJsonAsync("/api/v1/sessions/entry",
            new { SiteId = TestIsolationDataSeeder.OrgASiteId, RegistrationNumber = reg, VehicleMake = "Toyota" });

        entryResponse.EnsureSuccessStatusCode();
        var entry = await entryResponse.Content.ReadFromJsonAsync<VehicleEntryResult>(JsonOptions());
        entry.Should().NotBeNull();
        entry!.SessionId.Should().NotBeEmpty();

        var idempotencyKey = $"exit-{entry.SessionId}";
        var exitResponse = await client.PostAsJsonAsync("/api/v1/sessions/exit",
            new VehicleExitRequest(entry.SessionId, true, idempotencyKey));
        exitResponse.EnsureSuccessStatusCode();
        var exit = await exitResponse.Content.ReadFromJsonAsync<VehicleExitResult>(JsonOptions());
        exit.Should().NotBeNull();
        exit!.PaymentProcessed.Should().BeTrue();

        var duplicateExit = await client.PostAsJsonAsync("/api/v1/sessions/exit",
            new VehicleExitRequest(entry.SessionId, true, idempotencyKey));
        duplicateExit.EnsureSuccessStatusCode();
        var duplicateResult = await duplicateExit.Content.ReadFromJsonAsync<VehicleExitResult>(JsonOptions());
        duplicateResult!.SessionId.Should().Be(exit.SessionId);
        duplicateResult.ReceiptNumber.Should().Be(exit.ReceiptNumber);
    }

    [Fact]
    public async Task Authorization_GuardCannotCreateOrganization()
    {
        var client = await CreateAuthenticatedClient(TestIsolationDataSeeder.OrgAGuardEmail);
        var response = await client.PostAsJsonAsync("/api/v1/organizations",
            new { Name = "Hacker Org", DisplayName = "Hacker" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Authorization_CustomerCannotAccessOperationalDashboard()
    {
        var client = await CreateAuthenticatedClient(TestIsolationDataSeeder.OrgBCustomerEmail);
        var response = await client.GetAsync("/api/v1/dashboard/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Me_Endpoint_WorksWithBearerToken()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(TestIsolationDataSeeder.OrgAAdminEmail, TestIsolationDataSeeder.Password));
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        var meResponse = await client.GetAsync("/api/v1/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> CreateAuthenticatedClient(string email)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, TestIsolationDataSeeder.Password));
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };
}

[Collection(nameof(PostgresCollection))]
public class TenantIsolationTests : IAsyncLifetime
{
    private readonly ApiTestFactory _factory;

    public TenantIsolationTests(PostgresFixture postgres) => _factory = new ApiTestFactory(postgres);

    public Task InitializeAsync() => _factory.InitializeAsync();
    public Task DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task OrgA_User_SeesOnlyOrgA_Vehicles()
    {
        var client = await CreateAuthenticatedClient(TestIsolationDataSeeder.OrgAGuardEmail);
        var response = await client.GetAsync("/api/v1/vehicles");
        response.EnsureSuccessStatusCode();
        var vehicles = await response.Content.ReadFromJsonAsync<List<VehicleResponse>>(JsonOptions());
        vehicles.Should().NotBeNull();
        vehicles!.Should().Contain(v => v.Id == TestIsolationDataSeeder.OrgAVehicleId);
        vehicles.Should().NotContain(v => v.Id == TestIsolationDataSeeder.OrgBVehicleId);
    }

    [Fact]
    public async Task OrgA_User_CannotAccessOrgB_Session_ById()
    {
        var client = await CreateAuthenticatedClient(TestIsolationDataSeeder.OrgAGuardEmail);
        var response = await client.GetAsync($"/api/v1/sessions/{TestIsolationDataSeeder.OrgBSessionId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OrgA_User_CannotSeeOrgB_AuditLogs()
    {
        var client = await CreateAuthenticatedClient(TestIsolationDataSeeder.OrgAAdminEmail);
        var response = await client.GetAsync("/api/v1/dashboard/audit");
        response.EnsureSuccessStatusCode();
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLogResponse>>(JsonOptions());
        logs.Should().NotBeNull();
        logs!.Should().NotContain(l => l.Id == TestIsolationDataSeeder.OrgBAuditLogId);
    }

    [Fact]
    public async Task OrgB_User_CannotAccessOrgA_Vehicle_BySearch()
    {
        var client = await CreateAuthenticatedClient(TestIsolationDataSeeder.OrgBGuardEmail);
        var response = await client.GetAsync($"/api/v1/vehicles/search?registration={TestIsolationDataSeeder.OrgAVehicleReg}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<VehicleSearchResult>(JsonOptions());
        result!.Vehicle.Should().BeNull();
    }

    [Fact]
    public async Task WebClaimsPrincipalFactory_AddsOrganizationIdClaim()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var factory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(TestIsolationDataSeeder.OrgAGuardEmail);
        user.Should().NotBeNull();

        var principal = await factory.CreateAsync(user!);
        var orgClaim = principal.FindFirst(TenantConstants.OrganizationIdClaimType);
        orgClaim.Should().NotBeNull();
        orgClaim!.Value.Should().Be(TestIsolationDataSeeder.OrgAId.ToString());
    }

    private async Task<HttpClient> CreateAuthenticatedClient(string email)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, TestIsolationDataSeeder.Password));
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };

    private record VehicleResponse(Guid Id, string RegistrationNumber);
    private record AuditLogResponse(Guid Id, string? Details);
}

[Collection(nameof(PostgresCollection))]
public class ConcurrencyTests : IAsyncLifetime
{
    private readonly ApiTestFactory _factory;

    public ConcurrencyTests(PostgresFixture postgres) => _factory = new ApiTestFactory(postgres);

    public Task InitializeAsync() => _factory.InitializeAsync();
    public Task DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task ConcurrentEntry_ForSameVehicle_OnlyOneSucceeds()
    {
        var reg = $"DUP{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
        var client1 = await CreateAuthenticatedClient(TestIsolationDataSeeder.OrgAAdminEmail);
        var client2 = await CreateAuthenticatedClient(TestIsolationDataSeeder.OrgAAdminEmail);

        var payload = new { SiteId = TestIsolationDataSeeder.OrgASiteId, RegistrationNumber = reg };
        var task1 = client1.PostAsJsonAsync("/api/v1/sessions/entry", payload);
        var task2 = client2.PostAsJsonAsync("/api/v1/sessions/entry", payload);
        var results = await Task.WhenAll(task1, task2);

        var successCount = results.Count(r => r.IsSuccessStatusCode);
        successCount.Should().Be(1);
        results.Count(r => r.StatusCode == HttpStatusCode.BadRequest).Should().Be(1);
    }

    private async Task<HttpClient> CreateAuthenticatedClient(string email)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, TestIsolationDataSeeder.Password));
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };
}

[Collection(nameof(PostgresCollection))]
public class SignalRAuthorizationTests : IAsyncLifetime
{
    private readonly WebTestFactory _factory;

    public SignalRAuthorizationTests(PostgresFixture postgres) => _factory = new WebTestFactory(postgres);

    public Task InitializeAsync() => _factory.InitializeAsync();
    public Task DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task AnonymousUser_CannotConnectToHub()
    {
        var handler = _factory.Server.CreateHandler();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress!, "/hubs/parking"), options =>
            {
                options.HttpMessageHandlerFactory = _ => handler;
            })
            .Build();

        var act = async () => await hubConnection.StartAsync();
        await act.Should().ThrowAsync<Exception>();
    }
}
