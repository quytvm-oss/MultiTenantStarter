using System.Reflection;
using System.Text.Json;
using MessageBus.Exceptions;

namespace MessageBus;

public class MessageBusOptions
{
    public JsonSerializerOptions JsonSerializerOptions { get; } = new();
    
    public int SucceedMessageExpiredAfter { get; set; }
    
    public int FailedMessageExpiredAfter { get; set; }
    
    public int FailedRetryCount { get; set; }
    
    public int FailedRetryInterval { get; set; }
    
    public int FallbackWindowLookbackSeconds { get; set; }
    
    public int CollectorDeleteInterval { get; set; }
    
    public int ConsumerThreadCount { get; set; }
    
    public int SchedulerBatchSize { get; set; }

    public string Instance { get; set; } = "default";
    
    public string? GroupNamePrefix { get; set; }
    
    public string? TopicNamePrefix { get; set; }
    
    public string DefaultGroupName { get; set; }
    
    public Action<FailedInfo>? FailedThresholdCallback { get; set; }
    
    public IList<IMessageBusOptionsExtension> Extensions { get; }
    
    public bool EnableSubscriberParallelExecute { get; set; }
    
    public int SubscriberParallelExecuteThreadCount { get; set; }

    
    public int SubscriberParallelExecuteBufferFactor { get; set; }
    
    public bool EnablePublishParallelSend { get; set; } 
    
    internal List<(Assembly Assembly, Type? MarkerType)> Assemblies { get; } = [];
    
    internal List<Func<IConsumerRegistration>> ConsumerRegistrations { get; } = [];
    
    public MessageBusOptions()
    {
        SucceedMessageExpiredAfter = 24 * 3600;
        FailedMessageExpiredAfter = 15 * 24 * 3600;
        FailedRetryInterval = 60;
        FailedRetryCount = 50;
        ConsumerThreadCount = 1;
        EnablePublishParallelSend = false;
        EnableSubscriberParallelExecute = false;
        SubscriberParallelExecuteThreadCount = Environment.ProcessorCount;
        SubscriberParallelExecuteBufferFactor = 1;
        Extensions = new List<IMessageBusOptionsExtension>();
        DefaultGroupName = "system.queue.default";
        CollectorDeleteInterval = 300;
        FallbackWindowLookbackSeconds = 240;
        SchedulerBatchSize = 1000;
    }

    public void RegisterExtension(IMessageBusOptionsExtension extension)
    {
        ArgumentNullException.ThrowIfNull(extension);

        Extensions.Add(extension);
    }
}