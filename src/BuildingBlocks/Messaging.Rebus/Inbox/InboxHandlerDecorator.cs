// using Rebus.Handlers;
// using Rebus.Pipeline;

// namespace Messaging.Rebus.Inbox;

// internal sealed class InboxHandlerDecorator<TMessage>(
//     IHandleMessages<TMessage> inner,
//     IInboxStore inbox,
//     string queueName,
//     string handlerKey)
//     : IHandleMessages<TMessage>
// {
//     public async Task Handle(TMessage message)
//     {
//         var context = MessageContext.Current
//             ?? throw new InvalidOperationException(
//                 "Rebus MessageContext is unavailable.");

//         var messageId =
//             context.TransportMessage.Headers[Headers.MessageId];

//         var key = new InboxKey(
//             queueName,
//             handlerKey,
//             messageId);

//         var result = await inbox.TryAcquireAsync(key);

//         switch (result)
//         {
//             case InboxAcquireResult.Processed:
//                 return;

//             case InboxAcquireResult.Busy:
//                 // Một worker khác đang xử lý.
//                 return;

//             case InboxAcquireResult.Acquired:
//                 break;
//         }

//         try
//         {
//             await inner.Handle(message);

//             await inbox.MarkProcessedAsync(key);
//         }
//         catch (Exception exception)
//         {
//             await inbox.MarkFailedAsync(key, exception);
//             throw; // Rebus retry toàn message
//         }
//     }
// }