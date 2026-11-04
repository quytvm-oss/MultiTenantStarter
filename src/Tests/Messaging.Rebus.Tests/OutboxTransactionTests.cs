using Messaging.Rebus.Configuration;
using Messaging.Rebus.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Config.Outbox;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Messaging.Rebus.Tests;

public sealed class OutboxTransactionTests
{
    [PostgresFact]
    public async Task Borrowed_transaction_commits_business_and_multiple_messages_together()
    {
        await using var fixture = new PostgresFixture();
        await fixture.Store.InitializeAsync();
        var network = new InMemNetwork();
        using var bus = CreateBus(fixture, network);
        var module = new ModuleBus<TestModule>(new OutboxBus(bus, fixture.DataSource));
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        using (var scope = module.UseTransaction(connection, transaction))
        {
            Assert.NotNull(AmbientTransactionContext.Current);
            await module.Send("first");
            await module.Send("second");
            await scope.CompleteAsync();
            Assert.Equal(0, network.Count("output"));
        }
        Assert.Null(AmbientTransactionContext.Current);
        Assert.NotNull(transaction.Connection); // Scope did not dispose the caller's transaction.
        await transaction.CommitAsync();
        await Eventually(() => network.Count("output") == 2);
    }

    [PostgresFact]
    public async Task Rollback_discards_business_data_and_outgoing_messages()
    {
        await using var fixture = new PostgresFixture();
        await fixture.Store.InitializeAsync();
        await using (var create = fixture.DataSource.CreateCommand($"CREATE TABLE \"{fixture.Schema}\".business (id integer PRIMARY KEY);"))
            await create.ExecuteNonQueryAsync();
        var network = new InMemNetwork();
        using var bus = CreateBus(fixture, network);
        var module = new ModuleBus<TestModule>(new OutboxBus(bus, fixture.DataSource));
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        using (var scope = module.UseTransaction(connection, transaction))
        {
            await using var insert = new NpgsqlCommand($"INSERT INTO \"{fixture.Schema}\".business VALUES (1);", connection, transaction);
            await insert.ExecuteNonQueryAsync();
            await module.Send("must-not-be-delivered");
            await scope.CompleteAsync();
        }
        await transaction.RollbackAsync();
        await using var count = fixture.DataSource.CreateCommand($"SELECT (SELECT count(*) FROM \"{fixture.Schema}\".business) + (SELECT count(*) FROM \"{fixture.Schema}\".outbox);");
        Assert.Equal(0L, await count.ExecuteScalarAsync());
        Assert.Equal(0, network.Count("output"));
    }

    [PostgresFact]
    public async Task Standalone_send_uses_its_own_outbox_transaction()
    {
        await using var fixture = new PostgresFixture();
        await fixture.Store.InitializeAsync();
        var network = new InMemNetwork();
        using var bus = CreateBus(fixture, network);
        var module = new ModuleBus<TestModule>(new OutboxBus(bus, fixture.DataSource));
        await module.Send("standalone");
        await Eventually(() => network.Count("output") == 1);
        Assert.Null(AmbientTransactionContext.Current);
    }

    [PostgresFact]
    public async Task Actual_incoming_pipeline_persists_output_then_skips_duplicate()
    {
        await using var fixture = new PostgresFixture();
        await fixture.Store.InitializeAsync();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<InboxIncomingStep>>(NullLogger<InboxIncomingStep>.Instance);
        services.AddSingleton(fixture.DataSource);
        services.AddSingleton<IInboxStore>(fixture.Store);
        services.AddSingleton<HandlerState>();
        services.AddQueueHandler<ForwardingHandler>("input");
        services.AddQueueHandlerRegistry();
        await using var provider = services.BuildServiceProvider();
        var state = provider.GetRequiredService<HandlerState>();
        var network = new InMemNetwork();
        using var activator = new BuiltinHandlerActivator();
        using var raw = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "input"))
            .Outbox(o => o.StoreInPostgreSql(fixture.ConnectionString, fixture.Schema + ".outbox"))
            .Routing(r => r.TypeBased().Map<string>("output"))
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
                o.UseQueueHandlers(provider, "input", fixture.Options, () => state.Bus);
            })
            .Start();
        state.Bus = new ModuleBus<TestModule>(new OutboxBus(raw, fixture.DataSource));
        await raw.Advanced.Routing.Send("input", "incoming", new Dictionary<string, string> { [Headers.MessageId] = "same-message" });
        await Eventually(() => network.Count("output") == 1);
        var key = new InboxKey("input", "same-message");
        await EventuallyAsync(async () => await fixture.Store.TryAcquireAsync(key, Guid.NewGuid()) == InboxAcquireResult.Processed);
        await raw.Advanced.Routing.Send("input", "incoming", new Dictionary<string, string> { [Headers.MessageId] = "same-message" });
        // A marker sent after the duplicate establishes that the queue has drained.
        await raw.Advanced.Routing.Send("input", "marker", new Dictionary<string, string> { [Headers.MessageId] = "marker" });
        await state.Marker.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(1, state.Calls);
        Assert.Equal(1, network.Count("output"));
    }

    private static IBus CreateBus(PostgresFixture fixture, InMemNetwork network) =>
        Configure.With(new BuiltinHandlerActivator())
            .Transport(t => t.UseInMemoryTransport(network, "sender"))
            .Outbox(o => o.StoreInPostgreSql(fixture.ConnectionString, fixture.Schema + ".outbox"))
            .Routing(r => r.TypeBased().Map<string>("output"))
            .Options(o => o.SetNumberOfWorkers(0))
            .Start();

    [PostgresFact]
    public async Task Busy_delivery_is_deferred_with_the_same_id_then_runs_after_release()
    {
        await using var fixture = new PostgresFixture();
        await fixture.Store.InitializeAsync();
        fixture.Options.BusyRetryDelay = TimeSpan.FromMilliseconds(50);
        var key = new InboxKey("input", "busy-message");
        var owner = Guid.NewGuid();
        await fixture.Store.TryAcquireAsync(key, owner);
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<InboxIncomingStep>>(NullLogger<InboxIncomingStep>.Instance);
        services.AddSingleton(fixture.DataSource);
        services.AddSingleton<IInboxStore>(fixture.Store);
        services.AddSingleton<HandlerState>();
        services.AddQueueHandler<ForwardingHandler>("input");
        services.AddQueueHandlerRegistry();
        await using var provider = services.BuildServiceProvider();
        var state = provider.GetRequiredService<HandlerState>();
        var network = new InMemNetwork();
        using var activator = new BuiltinHandlerActivator();
        using var raw = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "input"))
            .Timeouts(t => t.StoreInMemory())
            .Outbox(o => o.StoreInPostgreSql(fixture.ConnectionString, fixture.Schema + ".outbox"))
            .Routing(r => r.TypeBased().Map<string>("output"))
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
                o.UseQueueHandlers(provider, "input", fixture.Options, () => state.Bus);
            })
            .Start();
        state.Bus = new ModuleBus<TestModule>(new OutboxBus(raw, fixture.DataSource));
        await raw.Advanced.Routing.Send("input", "incoming", new Dictionary<string, string> { [Headers.MessageId] = key.MessageId });
        await raw.Advanced.Routing.Send("input", "marker", new Dictionary<string, string> { [Headers.MessageId] = "marker" });
        await state.Marker.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(0, state.Calls);
        await fixture.Store.FailAsync(key, owner, "original worker stopped");
        await Eventually(() => network.Count("output") == 1);
        await EventuallyAsync(async () => await fixture.Store.TryAcquireAsync(key, Guid.NewGuid()) == InboxAcquireResult.Processed);
        Assert.Equal(1, state.Calls);
    }

    private static Task Eventually(Func<bool> condition) => EventuallyAsync(() => Task.FromResult(condition()));

    private static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!await condition()) await Task.Delay(20, timeout.Token);
    }

    public sealed class TestModule;
    public sealed class HandlerState
    {
        public IBus Bus { get; set; } = null!;
        public int Calls;
        public TaskCompletionSource Marker { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class ForwardingHandler(HandlerState state) : IHandleMessages<string>
    {
        public async Task Handle(string message)
        {
            if (message == "marker") { state.Marker.TrySetResult(); return; }
            Assert.NotNull(global::Rebus.Pipeline.MessageContext.Current);
            Interlocked.Increment(ref state.Calls);
            await state.Bus.Send("forwarded");
        }
    }
}
