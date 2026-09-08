namespace Web.MessageBus;

// public class RebusOptions
// {
//     public string QueueName { get; set; } = "hubmanagement";
//     public int NumberOfWorkers { get; set; } = 2;
//     public int MaxParallelism { get; set; } = 10;
//     public string MessagesTableName { get; set; } = "rebus_messages";
//     public string SubscriptionsTableName { get; set; } = "rebus_subscriptions";
//     public string OutboxTableName { get; set; } = "rebus_outbox";
// }

public class RebusOptions
{
    public string QueueName { get; set; } = "hubmanagement";
    public int NumberOfWorkers { get; set; } = 2;
    public int MaxParallelism { get; set; } = 10;

    public RabbitMqOptions RabbitMq { get; set; } = new();
    public RebusStorageOptions Storage { get; set; } = new();
}

public class RabbitMqOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    // Optional, nếu sau này cần cluster/failover
    // public string[] ConnectionStrings { get; set; } = Array.Empty<string>();
}

public class RebusStorageOptions
{
    public string SubscriptionsTableName { get; set; } = "rebus_subscriptions";
    public string OutboxTableName { get; set; } = "rebus_outbox";
    
    public string TimeoutsTableName { get; set; } = "rebus_timeout";
}