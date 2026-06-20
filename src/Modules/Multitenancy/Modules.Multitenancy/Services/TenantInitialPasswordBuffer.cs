using Microsoft.AspNetCore.DataProtection;

using Shared.Multitenancy;

using StackExchange.Redis;

namespace Modules.Multitenancy.Services;

internal sealed class TenantInitialPasswordBuffer : ITenantInitialPasswordBuffer
{
    private readonly IDatabase _db;
    private readonly IDataProtector _protector;

    public TenantInitialPasswordBuffer(
        IConnectionMultiplexer redis,
        IDataProtectionProvider dataProtection)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(dataProtection);

        _db = redis.GetDatabase();
        _protector = dataProtection.CreateProtector("tenant-initial-admin-password");
    }

    public void Store(string tenantId, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var encrypted = _protector.Protect(password);

        _db.StringSet(Key(tenantId), encrypted);
    }

    public string? TryConsume(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        RedisValue value = _db.StringGetDelete(Key(tenantId));

        if (!value.HasValue)
        {
            return null;
        }

        return _protector.Unprotect((string)value!);
    }

    private static string Key(string tenantId)
        => $"tenant:init-password:{tenantId}";
}