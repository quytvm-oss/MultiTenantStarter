using MessageBus.Model;
using MessageBus.Persistence;

namespace MessageBus;

public interface IPublisher
{
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets or sets the CAP transaction context object.
    /// </summary>
    IBusTransaction? Transaction { get; set; }
    
    Task PublishAsync<T>(T message, Action<PublishOptions>? configure = null, CancellationToken ct = default);
    
    void Publish<T>(T message, Action<PublishOptions>? configure = null);
}