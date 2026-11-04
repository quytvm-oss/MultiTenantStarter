namespace Messaging.Rebus.Inbox;

public sealed record InboxKey(string QueueName, string MessageId);
