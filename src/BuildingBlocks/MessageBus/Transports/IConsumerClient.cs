using MessageBus.Model;

namespace MessageBus.Transports;

public interface IConsumerClient : IAsyncDisposable
{
    BrokerAddress BrokerAddress { get; }
    
    Task<ICollection<string>> FetchTopicsAsync(IEnumerable<string> topicNames)
    {
        return Task.FromResult<ICollection<string>>(topicNames.ToList());
    }
    
    Task SubscribeAsync(IEnumerable<string> topics);
    
    Task ListeningAsync(TimeSpan timeout, CancellationToken cancellationToken);
    
    Task CommitAsync(object? sender);
    
    Task RejectAsync(object? sender);
    
    public Func<TransportContext, object?, Task>? OnMessageCallback { get; set; }
    
    public Action<LogMessageEventArgs>? OnLogCallback { get; set; }
}
