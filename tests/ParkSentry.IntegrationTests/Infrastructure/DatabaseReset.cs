using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkSentry.Infrastructure.Persistence;

namespace ParkSentry.IntegrationTests.Infrastructure;

/// <summary>
/// Resets the shared test database so integration test classes do not share mutated state.
/// </summary>
public static class DatabaseReset
{
    public static async Task ResetAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParkSentryDbContext>();

        // Truncate all application + Identity tables while keeping migration history.
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            DECLARE
                stmt text;
            BEGIN
                SELECT 'TRUNCATE TABLE ' || string_agg(format('%I.%I', schemaname, tablename), ', ') || ' RESTART IDENTITY CASCADE'
                INTO stmt
                FROM pg_tables
                WHERE schemaname = 'public'
                  AND tablename <> '__EFMigrationsHistory';

                IF stmt IS NOT NULL THEN
                    EXECUTE stmt;
                END IF;
            END $$;
            """);

        TestIsolationDataSeeder.ResetStaticIds();
        await TestIsolationDataSeeder.SeedAsync(services);
    }
}
