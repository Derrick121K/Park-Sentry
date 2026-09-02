using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace ParkSentry.IntegrationTests.Infrastructure;

/// <summary>
/// Shared PostgreSQL container for integration tests.
/// Uses an explicit image tag, deterministic credentials, and a bounded readiness wait.
/// Connection string always points at this container — never host localhost Postgres.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string Image = "postgres:16-alpine";
    public const string Database = "parksentry_test";
    public const string Username = "test";
    public const string Password = "test";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(2);

    private readonly PostgreSqlContainer _container;

    public PostgresFixture()
    {
        _container = new PostgreSqlBuilder(Image)
            .WithDatabase(Database)
            .WithUsername(Username)
            .WithPassword(Password)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilCommandIsCompleted("pg_isready", "-U", Username, "-d", Database))
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public string ContainerId => _container.Id;

    public async Task InitializeAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(StartupTimeout);
            await _container.StartAsync(cts.Token);
            await WaitUntilAcceptingConnectionsAsync(cts.Token);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Failed to start PostgreSQL test container. " +
                $"Image={Image}, Database={Database}, Username={Username}, " +
                $"ContainerId={_container.Id ?? "(not assigned)"}, " +
                $"State={_container.State}. " +
                $"Ensure Docker Desktop is running. Inner: {ex.GetType().Name}: {ex.Message}",
                ex);
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _container.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Warning: PostgreSQL test container disposal failed (Id={_container.Id}): {ex.Message}");
        }
    }

    private async Task WaitUntilAcceptingConnectionsAsync(CancellationToken cancellationToken)
    {
        var cs = ConnectionString;
        await using var conn = new Npgsql.NpgsqlConnection(cs);

        const int maxAttempts = 30;
        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    await conn.CloseAsync();

                await conn.OpenAsync(cancellationToken);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteScalarAsync(cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"PostgreSQL container started but did not accept connections within {StartupTimeout}. " +
            $"ContainerId={_container.Id}, Host={_container.Hostname}, Port={_container.GetMappedPublicPort(5432)}. " +
            $"Last error: {last?.Message}",
            last);
    }
}

[CollectionDefinition(nameof(PostgresCollection))]
public class PostgresCollection : ICollectionFixture<PostgresFixture>;
