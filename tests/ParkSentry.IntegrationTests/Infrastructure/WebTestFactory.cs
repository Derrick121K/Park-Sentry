extern alias Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ParkSentry.Infrastructure.Persistence;

namespace ParkSentry.IntegrationTests.Infrastructure;

public class WebTestFactory : WebApplicationFactory<Web::Program>, IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private bool _initialized;

    public WebTestFactory(PostgresFixture postgres) => _postgres = postgres;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.ConnectionString);
        builder.UseSetting("Jwt:Key", "TestJwtKey_AtLeast32CharactersLong_ForIntegration!");
        builder.UseSetting("Jwt:Issuer", "ParkSentry");
        builder.UseSetting("Jwt:Audience", "ParkSentry.Api");
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await DataSeeder.ApplyMigrationsAsync(Services);
        await TestIsolationDataSeeder.SeedAsync(Services);
        _initialized = true;
    }

    public new async Task DisposeAsync() => await base.DisposeAsync();
}
