using Messaging.Rebus.Configuration;
using Messaging.Rebus.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Messaging.Rebus.Tests;

public sealed class InboxPipelineTests
{
    [Fact]
    public void Duplicate_registration_is_rejected_without_partial_registration()
    {
        var services = new ServiceCollection();
        services.AddQueueHandler<Handler>("identity");
        var count = services.Count;
        var error = Assert.Throws<InvalidOperationException>(() => services.AddQueueHandler<OtherHandler>("IDENTITY"));
        Assert.Contains(nameof(Handler), error.Message);
        Assert.Contains(nameof(OtherHandler), error.Message);
        Assert.Equal(count, services.Count);
        services.AddQueueHandler<Handler>("other-queue");
    }

    [Fact]
    public void Registry_rejects_manually_registered_duplicates()
    {
        Assert.Throws<InvalidOperationException>(() => new RebusHandlerRegistry([
            new("identity", typeof(string), typeof(Handler)),
            new("IDENTITY", typeof(string), typeof(OtherHandler))]));
    }

    [Fact]
    public async Task Processed_message_skips_handler()
    {
        var store = new FakeStore { Result = InboxAcquireResult.Processed };
        using var transaction = new RebusTransactionScope();
        await Step(store).Process(Context(transaction), () => throw new Exception("Must not run"));
        Assert.False(store.Completed);
    }

    [Fact]
    public async Task Busy_message_throws_instead_of_acknowledging()
    {
        var store = new FakeStore { Result = InboxAcquireResult.Busy };
        using var transaction = new RebusTransactionScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Step(store).Process(Context(transaction), () => throw new Exception("Must not run")));
    }

    [Fact]
    public async Task Busy_message_is_deferred_and_never_marked_processed()
    {
        var store = new FakeStore { Result = InboxAcquireResult.Busy };
        using var transaction = new RebusTransactionScope();
        var deferred = false;
        var step = new InboxIncomingStep(store, new InboxOptions(), "identity", NullLogger<InboxIncomingStep>.Instance,
            delay => { deferred = true; Assert.True(delay > TimeSpan.Zero); return Task.CompletedTask; });
        await step.Process(Context(transaction), () => throw new Exception("Must not run"));
        await transaction.CompleteAsync();
        Assert.True(deferred);
        Assert.False(store.Completed);
    }

    [Fact]
    public async Task Inbox_completes_only_after_outgoing_commit()
    {
        var store = new FakeStore();
        using var transaction = new RebusTransactionScope();
        var outgoingSaved = false;
        await Step(store).Process(Context(transaction), () =>
        {
            transaction.TransactionContext.OnCommit(_ => { outgoingSaved = true; return Task.CompletedTask; });
            return Task.CompletedTask;
        });
        Assert.False(store.Completed);
        store.OnComplete = () => Assert.True(outgoingSaved);
        await transaction.CompleteAsync();
        Assert.True(store.Completed);
    }

    [Fact]
    public async Task Handler_failure_releases_lease_and_rethrows()
    {
        var store = new FakeStore();
        using var transaction = new RebusTransactionScope();
        await Assert.ThrowsAsync<ArgumentException>(() => Step(store).Process(Context(transaction),
            () => throw new ArgumentException("handler failed")));
        Assert.True(store.Failed);
        Assert.False(store.Completed);
    }

    [Fact]
    public async Task Outgoing_commit_failure_does_not_mark_inbox_processed()
    {
        var store = new FakeStore();
        using var transaction = new RebusTransactionScope();
        await Step(store).Process(Context(transaction), () =>
        {
            transaction.TransactionContext.OnCommit(_ => throw new ArgumentException("outbox failed"));
            return Task.CompletedTask;
        });
        await Assert.ThrowsAsync<ArgumentException>(() => transaction.CompleteAsync());
        Assert.False(store.Completed);
        Assert.True(store.Failed);
    }

    [Fact]
    public async Task Lost_ownership_prevents_successful_commit()
    {
        var store = new FakeStore { OwnsLease = false };
        using var transaction = new RebusTransactionScope();
        await Step(store).Process(Context(transaction), () => Task.CompletedTask);
        await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CompleteAsync());
    }

    [Fact]
    public async Task Heartbeat_renews_during_long_handler()
    {
        var store = new FakeStore();
        using var transaction = new RebusTransactionScope();
        var options = new InboxOptions { RenewalInterval = TimeSpan.FromMilliseconds(10) };
        var step = new InboxIncomingStep(store, options, "identity", NullLogger<InboxIncomingStep>.Instance);
        await step.Process(Context(transaction), () => store.Renewed.Task.WaitAsync(TimeSpan.FromSeconds(3)));
        await transaction.CompleteAsync();
        Assert.True(store.Completed);
    }

    private static InboxIncomingStep Step(FakeStore store) =>
        new(store, new InboxOptions(), "identity", NullLogger<InboxIncomingStep>.Instance);

    private static IncomingStepContext Context(RebusTransactionScope scope) =>
        new(new TransportMessage(new() { [Headers.MessageId] = "message-1" }, []), scope.TransactionContext);

    public sealed class Handler : IHandleMessages<string>
    {
        public Task Handle(string message) => Task.CompletedTask;
    }
    public sealed class OtherHandler : IHandleMessages<string>
    {
        public Task Handle(string message) => Task.CompletedTask;
    }

    private sealed class FakeStore : IInboxStore
    {
        public InboxAcquireResult Result { get; init; } = InboxAcquireResult.Acquired;
        public bool Completed { get; private set; }
        public bool Failed { get; private set; }
        public bool OwnsLease { get; init; } = true;
        public Action? OnComplete { get; set; }
        public TaskCompletionSource Renewed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<InboxAcquireResult> TryAcquireAsync(InboxKey key, Guid lockId, CancellationToken cancellationToken = default) => Task.FromResult(Result);
        public Task<bool> RenewAsync(InboxKey key, Guid lockId, CancellationToken cancellationToken = default)
        {
            Renewed.TrySetResult();
            return Task.FromResult(OwnsLease);
        }
        public Task<bool> CompleteAsync(InboxKey key, Guid lockId, CancellationToken cancellationToken = default)
        {
            OnComplete?.Invoke();
            Completed = OwnsLease;
            return Task.FromResult(OwnsLease);
        }
        public Task FailAsync(InboxKey key, Guid lockId, string error, CancellationToken cancellationToken = default)
        {
            Failed = true;
            return Task.CompletedTask;
        }
        public Task<int> CleanupAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
