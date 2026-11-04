using Microsoft.Extensions.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Messaging.Rebus.Inbox;

public sealed class InboxIncomingStep(
    IInboxStore store, InboxOptions options, string queueName, ILogger<InboxIncomingStep> logger,
    Func<TimeSpan, Task>? deferCurrentMessage = null) : IIncomingStep
{
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var message = context.Load<TransportMessage>();
        if (!message.Headers.TryGetValue(Headers.MessageId, out var messageId) || string.IsNullOrWhiteSpace(messageId))
            throw new InvalidOperationException("Inbox requires a non-empty Rebus message ID.");

        var key = new InboxKey(queueName, messageId);
        var owner = Guid.NewGuid();
        var result = await store.TryAcquireAsync(key, owner).ConfigureAwait(false);
        if (result == InboxAcquireResult.Processed) return;
        if (result == InboxAcquireResult.Busy)
        {
            if (deferCurrentMessage is null)
                throw new InvalidOperationException($"Inbox message '{messageId}' on queue '{queueName}' is busy and no deferral callback is configured.");
            // Rebus must persist/schedule the deferred copy before ACKing this delivery.
            await deferCurrentMessage(options.BusyRetryDelay).ConfigureAwait(false);
            return;
        }

        var lease = new Lease(store, key, owner, options.RenewalInterval, logger);
        var transaction = context.Load<ITransactionContext>();
        transaction.OnRollback(_ => lease.FailAsync("Rebus transaction rolled back."));
        transaction.OnNack(_ => lease.FailAsync("Rebus transaction was not acknowledged."));
        transaction.OnDisposed(_ => lease.Dispose());

        try
        {
            await next().ConfigureAwait(false);
            // Register AFTER downstream processing: outgoing outbox commit callbacks
            // must finish before a message can be marked Processed.
            transaction.OnCommit(_ => lease.CompleteAsync());
        }
        catch (Exception exception)
        {
            await lease.FailAsync(exception.ToString()).ConfigureAwait(false);
            throw;
        }
    }

    private sealed class Lease : IDisposable
    {
        private readonly IInboxStore _store;
        private readonly InboxKey _key;
        private readonly Guid _owner;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _heartbeat;
        private bool _finished;
        private bool _disposed;

        public Lease(IInboxStore store, InboxKey key, Guid owner, TimeSpan interval, ILogger logger)
        {
            _store = store;
            _key = key;
            _owner = owner;
            _logger = logger;
            _heartbeat = RenewAsync(interval);
        }

        private async Task RenewAsync(TimeSpan interval)
        {
            using var timer = new PeriodicTimer(interval);
            try
            {
                while (await timer.WaitForNextTickAsync(_stop.Token).ConfigureAwait(false))
                {
                    if (!await _store.RenewAsync(_key, _owner, _stop.Token).ConfigureAwait(false))
                        throw new InvalidOperationException($"Inbox lease lost for '{_key}'.");
                }
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        }

        public async Task CompleteAsync()
        {
            await _stop.CancelAsync().ConfigureAwait(false);
            await _heartbeat.ConfigureAwait(false);
            if (!await _store.CompleteAsync(_key, _owner).ConfigureAwait(false))
                throw new InvalidOperationException($"Cannot complete inbox message '{_key}': lease expired or ownership changed.");
            _finished = true;
        }

        public async Task FailAsync(string error)
        {
            if (_finished) return;
            await _stop.CancelAsync().ConfigureAwait(false);
            try { await _heartbeat.ConfigureAwait(false); }
            catch (Exception exception) { _logger.LogWarning(exception, "Inbox heartbeat failed for {InboxKey}.", _key); }
            try
            {
                await _store.FailAsync(_key, _owner, error).ConfigureAwait(false);
                _finished = true;
            }
            catch (Exception exception)
            {
                // Preserve the original handler/commit exception; the lease will expire.
                _logger.LogWarning(exception, "Could not release inbox lease for {InboxKey}.", _key);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _stop.Cancel();
            try { _heartbeat.GetAwaiter().GetResult(); }
            catch (Exception exception) { _logger.LogWarning(exception, "Inbox heartbeat stopped for {InboxKey}.", _key); }
            _stop.Dispose();
            _disposed = true;
        }
    }
}
