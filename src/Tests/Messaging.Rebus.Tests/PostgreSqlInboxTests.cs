using Messaging.Rebus.Inbox;

namespace Messaging.Rebus.Tests;

public sealed class PostgreSqlInboxTests
{
    [PostgresFact]
    public async Task Concurrent_claims_have_one_owner_and_queue_identity_is_case_insensitive()
    {
        await using var fixture = new PostgresFixture();
        await fixture.Store.InitializeAsync();
        var claims = await Task.WhenAll(Enumerable.Range(0, 12).Select(i => fixture.Store.TryAcquireAsync(
            new(i % 2 == 0 ? "identity" : "IDENTITY", "message-1"), Guid.NewGuid())));
        Assert.Single(claims, x => x == InboxAcquireResult.Acquired);
        Assert.Equal(11, claims.Count(x => x == InboxAcquireResult.Busy));
        Assert.Equal(InboxAcquireResult.Acquired, await fixture.Store.TryAcquireAsync(new("webhooks", "message-1"), Guid.NewGuid()));
    }

    [PostgresFact]
    public async Task Failed_claim_can_retry_but_stale_owner_cannot_complete_or_renew()
    {
        await using var fixture = new PostgresFixture();
        await fixture.Store.InitializeAsync();
        var key = new InboxKey("identity", "message-1");
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        await fixture.Store.TryAcquireAsync(key, first);
        await fixture.Store.FailAsync(key, first, "handler failed");
        Assert.Equal(InboxAcquireResult.Acquired, await fixture.Store.TryAcquireAsync(key, second));
        Assert.False(await fixture.Store.CompleteAsync(key, first));
        Assert.False(await fixture.Store.RenewAsync(key, first));
        await fixture.Store.FailAsync(key, first, "stale failure");
        Assert.True(await fixture.Store.CompleteAsync(key, second));
        Assert.Equal(InboxAcquireResult.Processed, await fixture.Store.TryAcquireAsync(key, Guid.NewGuid()));
    }

    [PostgresFact]
    public async Task Expired_lease_is_reclaimed_and_cleanup_keeps_unfinished_messages()
    {
        await using var fixture = new PostgresFixture();
        await fixture.Store.InitializeAsync();
        var key = new InboxKey("identity", "message-1");
        var first = Guid.NewGuid();
        await fixture.Store.TryAcquireAsync(key, first);
        await using (var expire = fixture.DataSource.CreateCommand($"UPDATE \"{fixture.Schema}\".inbox_messages SET locked_until_utc = now() - interval '1 second';"))
            await expire.ExecuteNonQueryAsync();
        Assert.False(await fixture.Store.RenewAsync(key, first));
        var second = Guid.NewGuid();
        Assert.Equal(InboxAcquireResult.Acquired, await fixture.Store.TryAcquireAsync(key, second));
        Assert.False(await fixture.Store.CompleteAsync(key, first));
        Assert.True(await fixture.Store.CompleteAsync(key, second));
        await fixture.Store.TryAcquireAsync(new("identity", "unfinished"), Guid.NewGuid());
        await using (var age = fixture.DataSource.CreateCommand($"UPDATE \"{fixture.Schema}\".inbox_messages SET processed_at_utc = now() - interval '31 days' WHERE status = 'Processed';"))
            await age.ExecuteNonQueryAsync();
        Assert.Equal(1, await fixture.Store.CleanupAsync());
        Assert.Equal(InboxAcquireResult.Busy, await fixture.Store.TryAcquireAsync(new("identity", "unfinished"), Guid.NewGuid()));
    }
}
