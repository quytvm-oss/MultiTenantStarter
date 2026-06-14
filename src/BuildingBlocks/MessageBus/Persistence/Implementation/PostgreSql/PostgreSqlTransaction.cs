using System.Data;
using System.Data.Common;
using System.Diagnostics;

using MessageBus.Contracts;

using Microsoft.EntityFrameworkCore.Storage;

namespace MessageBus.Persistence.Implementation.PostgreSql;

public class PostgreSqlTransaction: BusTransaction
{
    public PostgreSqlTransaction(IDispatcher dispatcher) : base(dispatcher)
    {
    }

    public override void Commit()
    {
        switch (DbTransaction)
        {
            case IDbTransaction dbTransaction:
                dbTransaction.Commit();
                break;
            case IDbContextTransaction dbContextTransaction:
                dbContextTransaction.Commit();
                break;
        }

        Flush();
    }

    public override async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Debug.Assert(DbTransaction != null);

        switch (DbTransaction)
        {
            case DbTransaction dbTransaction:
                await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                break;
            case IDbContextTransaction dbContextTransaction:
                await dbContextTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                break;
        }

        await FlushAsync();
    }

    public override void Rollback()
    {
        Debug.Assert(DbTransaction != null);

        switch (DbTransaction)
        {
            case IDbTransaction dbTransaction:
                dbTransaction.Rollback();
                break;
            case IDbContextTransaction dbContextTransaction:
                dbContextTransaction.Rollback();
                break;
        }
    }

    public override async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        Debug.Assert(DbTransaction != null);

        switch (DbTransaction)
        {
            case DbTransaction dbTransaction:
                await dbTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                break;
            case IDbContextTransaction dbContextTransaction:
                await dbContextTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                break;
        }
    }
}
