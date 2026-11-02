using System;
using System.Collections.Generic;
using System.Text;

namespace Messaging.Rebus.Configuration;

public class RebusOptions
{
    public string QueueName { get; set; } = "multitenant";
    public int NumberOfWorkers { get; set; } = 2;
    public int MaxParallelism { get; set; } = 10;
    public DatabaseSettings Database { get; set; } = new();
    public RabbitMqOptions RabbitMq { get; set; } = new();
    public RebusStorageOptions Storage { get; set; } = new();
}

public class RabbitMqOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class RebusStorageOptions
{
    public string SubscriptionsTableName { get; set; } = "rebus_subscriptions";
    public string OutboxTableName { get; set; } = "rebus_outbox";
    public string TimeoutsTableName { get; set; } = "rebus_timeout";
}