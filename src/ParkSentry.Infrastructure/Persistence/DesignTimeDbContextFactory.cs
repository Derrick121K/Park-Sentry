using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using ParkSentry.Infrastructure.Services;

namespace ParkSentry.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ParkSentryDbContext>
{
    public ParkSentryDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../ParkSentry.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=parksentry;Username=parksentry;Password=parksentry_dev";

        var optionsBuilder = new DbContextOptionsBuilder<ParkSentryDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ParkSentryDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : Application.Interfaces.ITenantContext
    {
        public Guid? OrganizationId => null;
        public string? UserId => null;
        public bool IsSuperAdmin => true;
    }
}
