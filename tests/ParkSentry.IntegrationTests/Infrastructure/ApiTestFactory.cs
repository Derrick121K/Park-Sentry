using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ParkSentry.Infrastructure.Persistence;

namespace ParkSentry.IntegrationTests.Infrastructure;

public class ApiTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private bool _initialized;

    public ApiTestFactory(PostgresFixture postgres) => _postgres = postgres;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.ConnectionString);
        builder.UseSetting("Jwt:Key", "TestJwtKey_AtLeast32CharactersLong_ForIntegration!");
        builder.UseSetting("Jwt:Issuer", "ParkSentry");
        builder.UseSetting("Jwt:Audience", "ParkSentry.Api");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
        builder.UseSetting("Integrations:Mode", "Demo");
        builder.UseSetting("Payments:Provider", "Mock");
        builder.UseSetting("Scanning:Provider", "Demo");
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await DataSeeder.ApplyMigrationsAsync(Services);
        await DatabaseReset.ResetAndSeedAsync(Services);
        _initialized = true;
    }

    public new async Task DisposeAsync() => await base.DisposeAsync();
}
