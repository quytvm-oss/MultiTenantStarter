using Modules.Auditing.Contracts;

namespace Modules.Auditing.Core;

public static class Audit
{
    private static IAuditPublisher Publisher;
    
    private static IAuditSerializer Serializer;


    private sealed class NoopPublisher : IAuditPublisher

    {
        public ValueTask PublishAsync(IAuditEvent auditEvent, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public IAuditScope CurrentScope { get; }
    }
}