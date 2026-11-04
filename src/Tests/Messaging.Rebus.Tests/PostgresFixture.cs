using Messaging.Rebus.Inbox;
using Npgsql;

namespace Messaging.Rebus.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MESSAGING_TEST_CONNECTION")))
            Skip = "Set MESSAGING_TEST_CONNECTION to run PostgreSQL integration tests.";
    }
}

internal sealed class PostgresFixture : IAsyncDisposable
{
    public string Schema { get; } = "messaging_tests_" + Guid.NewGuid().ToString("N");
    public string ConnectionString { get; } = Environment.GetEnvironmentVariable("MESSAGING_TEST_CONNECTION")!;
    public NpgsqlDataSource DataSource { get; }
    public InboxOptions Options { get; }
    public PostgreSqlInboxStore Store { get; }

    public PostgresFixture()
    {
        DataSource = NpgsqlDataSource.Create(ConnectionString);
        Options = new InboxOptions { SchemaName = Schema };
        Store = new(DataSource, Options);
    }

    public async ValueTask DisposeAsync()
    {
        // This fixture owns only its unique schema; never remove application schemas.
        if (!Schema.StartsWith("messaging_tests_", StringComparison.Ordinal) || Schema.Length != 48)
            throw new InvalidOperationException("Invalid test schema.");
        await using var command = DataSource.CreateCommand($"DROP SCHEMA IF EXISTS \"{Schema}\" CASCADE;");
        await command.ExecuteNonQueryAsync();
        await DataSource.DisposeAsync();
    }
}
