using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageBus.Transports.Implementation.RabbitMq;

public class RabbitMQOptions
{
    public const string DefaultPass = "guest";

 
    public const string DefaultUser = "guest";
    
    
    public const string DefaultVHost = "/";

    
    public const string DefaultExchangeName = "default.router";

   
    public const string ExchangeType = "topic";
    
    
    public string HostName { get; set; } = "localhost";

  
    public string Password { get; set; } = DefaultPass;

   
    public string UserName { get; set; } = DefaultUser;

   
    public string VirtualHost { get; set; } = DefaultVHost;

   
    public string ExchangeName { get; set; } = DefaultExchangeName;

 
    public bool PublishConfirms { get; set; }

   
    public int Port { get; set; } = -1;

   
    public QueueArgumentsOptions QueueArguments { get; set; } = new();

    
    public QueueRabbitOptions QueueOptions { get; set; } = new();

  
    public Func<BasicDeliverEventArgs, IServiceProvider, List<KeyValuePair<string, string>>>? CustomHeadersBuilder
    {
        get;
        set;
    }
    
    public Action<ConnectionFactory>? ConnectionFactoryOptions { get; set; }

    
    public BasicQos? BasicQosOptions { get; set; } = null;
}

public class QueueArgumentsOptions
{
   
    public string QueueMode { get; set; } = null!;

  
    public int MessageTTL { get; set; } = 864000000;

  
    public string QueueType { get; set; } = null!;
}

public class BasicQos
{
    
    public BasicQos(ushort prefetchCount, bool global = false)
    {
        PrefetchCount = prefetchCount;
        Global = global;
    }

    
    public ushort PrefetchCount { get; }

   
    public bool Global { get; }
}

public class QueueRabbitOptions
{
    public bool Durable { get; set; } = true;
    public bool Exclusive { get; set; } = false;
    public bool AutoDelete { get; set; } = false;
}