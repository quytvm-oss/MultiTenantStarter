using System.Data;
using MessageBus.Model;

namespace MessageBus.Persistence;


internal sealed class BusTransactionHolder
{
    /// <summary>
    /// Gets or sets the CAP transaction associated with the current context.
    /// </summary>
    public IBusTransaction? Transaction;
}

public interface IBusTransaction : IDisposable
{
    object DbTransaction { get; set; }
    
    bool AutoCommit { get; set; }
    
    void Commit();
    
    Task CommitAsync(CancellationToken cancellationToken = default);
    
    Task RollbackAsync(CancellationToken cancellationToken = default);
    
    void Rollback();
    
    void AddToBuffer(MessageContext message);
}