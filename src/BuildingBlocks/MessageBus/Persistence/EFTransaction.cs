using System.Data.Common;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace MessageBus.Persistence;

internal class EFTransaction: IDbContextTransaction, IInfrastructure<DbTransaction>
{
    private readonly IBusTransaction _transaction;

    public EFTransaction(IBusTransaction transaction)
    {
        _transaction = transaction;
        var dbContextTransaction = (IDbContextTransaction)_transaction.DbTransaction!;
        TransactionId = dbContextTransaction.TransactionId;
    }

    public void Dispose()
    {
        _transaction.Dispose();
    }

    public void Commit()
    {
        _transaction.Commit();
    }

    public void Rollback()
    {
        _transaction.Rollback();
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.CommitAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.RollbackAsync(cancellationToken);
    }

    public Guid TransactionId { get; }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(Task.Run(() => _transaction.Dispose()));
    }

    public DbTransaction Instance
    {
        get
        {
            var dbContextTransaction = (IDbContextTransaction)_transaction.DbTransaction!;
            return dbContextTransaction.GetDbTransaction();
        }
    }
}