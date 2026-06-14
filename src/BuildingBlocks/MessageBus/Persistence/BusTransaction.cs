using System.Collections.Concurrent;
using System.Globalization;

using MessageBus.Constants;
using MessageBus.Contracts;
using MessageBus.Model;

namespace MessageBus.Persistence;

public abstract class BusTransaction : IBusTransaction
{
    private readonly ConcurrentQueue<MessageContext> _buffer;
    private readonly IDispatcher _dispatcher;

    public BusTransaction(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _buffer = new ConcurrentQueue<MessageContext>();
    }

    public virtual object? DbTransaction { get; set; }
    public bool AutoCommit { get; set; }

    public abstract void Rollback();

    public virtual void AddToBuffer(MessageContext message)
    {
        _buffer.Enqueue(message);
    }

    public abstract void Commit();

    public abstract Task CommitAsync(CancellationToken cancellationToken = default);

    public abstract Task RollbackAsync(CancellationToken cancellationToken = default);
    
    protected virtual void Flush()
    {
        FlushAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    protected virtual async Task FlushAsync()
    {
        while (!_buffer.IsEmpty)
        {
            if (_buffer.TryDequeue(out var message))
            {
                var isDelayMessage = message.Origin.Headers.ContainsKey(HeaderConstant.DelayTime);
                if (isDelayMessage)
                {
                    await _dispatcher.EnqueueToScheduler(message, DateTime.Parse(message.Origin.Headers[HeaderConstant.SentTime]!, CultureInfo.InvariantCulture)).ConfigureAwait(false);
                }
                else
                {
                    await _dispatcher.EnqueueToPublish(message).ConfigureAwait(false);
                }
            }
        }
    }

    public virtual void Dispose()
    {
        (DbTransaction as IDisposable)?.Dispose();
        DbTransaction = null;
    }
}