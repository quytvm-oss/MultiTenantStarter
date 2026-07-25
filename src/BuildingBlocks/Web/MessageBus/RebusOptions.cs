namespace Web.MessageBus;

public class RebusOptions
{
    public string QueueName { get; set; } = "hubmanagement";
    public int NumberOfWorkers { get; set; } = 2;
    public int MaxParallelism { get; set; } = 10;
    public string MessagesTableName { get; set; } = "rebus_messages";
    public string SubscriptionsTableName { get; set; } = "rebus_subscriptions";
    public string OutboxTableName { get; set; } = "rebus_outbox";
}