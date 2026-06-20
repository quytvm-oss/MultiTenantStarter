using System.Data;
using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus.Persistence.Implementation.PostgreSql;


public static class BusTransactionExtensions
{
    // public static IBusTransaction BeginTransaction(this IDbConnection connection, IPublisher publisher,
    //     IsolationLevel isolationLevel = IsolationLevel.Unspecified, bool autoCommit = false)
    // {
    //     if (connection.State == ConnectionState.Closed)
    //         connection.Open();
    //
    //     var dbTransaction = connection.BeginTransaction(isolationLevel);
    //
    //     var busTransaction = publisher.ServiceProvider
    //         .GetRequiredService<BusTransaction>();
    //
    //     busTransaction.DbTransaction = dbTransaction;
    //     busTransaction.AutoCommit = autoCommit;
    //     publisher.Transaction = busTransaction;
    //
    //     return busTransaction;
    // }
    //
    // public static async Task<IBusTransaction> BeginTransactionAsync(this DbConnection connection, IPublisher publisher,
    //     IsolationLevel isolationLevel = IsolationLevel.Unspecified, bool autoCommit = false, 
    //     CancellationToken cancellationToken = default)
    // {
    //     if (connection.State == ConnectionState.Closed)
    //         await connection.OpenAsync(cancellationToken);
    //
    //     var dbTransaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken);
    //
    //     var busTransaction = publisher.ServiceProvider
    //         .GetRequiredService<BusTransaction>();
    //
    //     busTransaction.DbTransaction = dbTransaction;
    //     busTransaction.AutoCommit = autoCommit;
    //     publisher.Transaction = busTransaction;
    //
    //     return busTransaction;
    // }
    
     public static IBusTransaction BeginTransaction(this IDbConnection dbConnection,
         IBusPublisher publisher, bool autoCommit = false)
    {
        return BeginTransaction(dbConnection, IsolationLevel.Unspecified, publisher, autoCommit);
    }

   
    public static IBusTransaction BeginTransaction(this IDbConnection dbConnection,
        IsolationLevel isolationLevel, IBusPublisher publisher, bool autoCommit = false)
    {
        if (dbConnection.State == ConnectionState.Closed) dbConnection.Open();
        var dbTransaction = dbConnection.BeginTransaction(isolationLevel);

        publisher.Transaction = ActivatorUtilities.CreateInstance<PostgreSqlTransaction>(publisher.ServiceProvider);
        publisher.Transaction.DbTransaction = dbTransaction;
        publisher.Transaction.AutoCommit = autoCommit;

        return publisher.Transaction;
    }

    
    public static ValueTask<IBusTransaction> BeginTransactionAsync(this IDbConnection dbConnection,
        IBusPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
    {
        return BeginTransactionAsync(dbConnection, IsolationLevel.Unspecified, publisher, autoCommit, cancellationToken);
    }

    
    public static ValueTask<IBusTransaction> BeginTransactionAsync(this IDbConnection dbConnection,
        IsolationLevel isolationLevel, IBusPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
    {
        if (dbConnection.State == ConnectionState.Closed) ((DbConnection)dbConnection).OpenAsync(cancellationToken).GetAwaiter().GetResult();
        var dbTransaction = ((DbConnection)dbConnection).BeginTransactionAsync(isolationLevel, cancellationToken).AsTask().GetAwaiter().GetResult();
 
        publisher.Transaction = ActivatorUtilities.CreateInstance<PostgreSqlTransaction>(publisher.ServiceProvider);
        publisher.Transaction.DbTransaction = dbTransaction;
        publisher.Transaction.AutoCommit = autoCommit;

        return ValueTask.FromResult(publisher.Transaction);
    }

    
    public static IDbContextTransaction BeginTransaction(this DatabaseFacade database,
        IBusPublisher publisher, bool autoCommit = false)
    {
        return BeginTransaction(database, IsolationLevel.Unspecified, publisher, autoCommit);
    }

   
    public static IDbContextTransaction BeginTransaction(this DatabaseFacade database,
        IsolationLevel isolationLevel, IBusPublisher publisher, bool autoCommit = false)
    {
        var trans = database.BeginTransaction(isolationLevel);
        publisher.Transaction = ActivatorUtilities.CreateInstance<PostgreSqlTransaction>(publisher.ServiceProvider);
        publisher.Transaction.DbTransaction = trans;
        publisher.Transaction.AutoCommit = autoCommit;
        return new EFTransaction(publisher.Transaction);
    }

    
    public static Task<IDbContextTransaction> BeginTransactionAsync(this DatabaseFacade database,
        IBusPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
    {
        return BeginTransactionAsync(database, IsolationLevel.Unspecified, publisher, autoCommit, cancellationToken);
    }
    
    public static Task<IDbContextTransaction> BeginTransactionAsync(this DatabaseFacade database,
        IsolationLevel isolationLevel, IBusPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
    {
        var trans = database.BeginTransactionAsync(isolationLevel, cancellationToken).GetAwaiter().GetResult();
        publisher.Transaction = ActivatorUtilities.CreateInstance<PostgreSqlTransaction>(publisher.ServiceProvider);
        publisher.Transaction.DbTransaction = trans;
        publisher.Transaction.AutoCommit = autoCommit;
        return Task.FromResult<IDbContextTransaction>(new EFTransaction(publisher.Transaction));
    }
}