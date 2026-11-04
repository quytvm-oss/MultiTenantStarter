using System.Data;
using Npgsql;
using Rebus.Config.Outbox;
using Rebus.Transport;
using Rebus.Pipeline;

namespace Messaging.Rebus;

/// <summary>
/// Flushes outgoing messages into a borrowed transaction. CompleteAsync must be
/// called before the caller commits it. This scope never disposes or commits it.
/// </summary>
public sealed class ModuleBusTransactionScope : IDisposable
{
    private readonly RebusTransactionScope _scope;
    private bool _completed;
    private bool _disposed;
    private const string ScopeKey = "messaging-module-transaction";

    public ModuleBusTransactionScope(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        if (connection.State != ConnectionState.Open || transaction.Connection != connection)
            throw new ArgumentException("An open connection and its active transaction are required.");

        var previous = AmbientTransactionContext.Current;
        if (previous?.Items.ContainsKey(ScopeKey) == true)
            throw new InvalidOperationException("Nested module transaction scopes are not supported.");

        // Construction is synchronous so Rebus' AsyncLocal flows into the caller.
        _scope = new RebusTransactionScope();
        try
        {
            _scope.UseOutbox(connection, transaction);
            _scope.TransactionContext.Items[ScopeKey] = true;
            if (previous?.Items.TryGetValue(StepContext.StepContextKey, out var incoming) == true)
                _scope.TransactionContext.Items[StepContext.StepContextKey] = incoming;
        }
        catch
        {
            _scope.Dispose();
            throw;
        }
    }

    public async Task CompleteAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
            throw new InvalidOperationException("The messaging transaction scope has already completed.");
        await _scope.CompleteAsync().ConfigureAwait(false);
        _completed = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _scope.Dispose();
        _disposed = true;
    }
}
