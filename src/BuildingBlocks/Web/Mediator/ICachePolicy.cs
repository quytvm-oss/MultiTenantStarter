using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Mediator;

namespace Web.Mediator;

public interface ICachePolicy<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    TimeSpan? AbsoluteExpirationRelativeToNow { get; }
    IEnumerable<string>? Tags => null;
    string GetCacheKey(TRequest request) => CacheKeyGenerator.Generate(request);
}

public static class CacheKeyGenerator
{
    public static string Generate<TRequest>(TRequest request)
    {
        var serialized = JsonSerializer.Serialize(request);
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(serialized)));
        return $"{typeof(TRequest).Name}:{hash}";
    }
}