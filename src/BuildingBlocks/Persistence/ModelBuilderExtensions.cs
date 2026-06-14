using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Persistence;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Appends a global query filter to all entities implementing a specific interface.
    /// This method allows adding filters that are applied automatically whenever querying
    /// entities of types that implement the specified interface.
    /// </summary>
    /// <typeparam name="TInterface">
    /// The type of the interface that the entities must implement to have the filter applied.
    /// </typeparam>
    /// <param name="modelBuilder">
    /// The <see cref="ModelBuilder"/> instance used to configure the EF Core model.
    /// </param>
    /// <param name="filterName">
    /// A unique name for the query filter. This is used to identify the filter and avoid conflicts.
    /// </param>
    /// <param name="filter">
    /// An expression representing the filter. The filter will be applied to all entities of types
    /// that implement <typeparamref name="TInterface"/>.
    /// </param>
    /// <returns>
    /// The modified <see cref="ModelBuilder"/> instance.
    /// </returns>
    public static ModelBuilder AppendGlobalQueryFilter<TInterface>(
        this ModelBuilder modelBuilder,
        string filterName,
        Expression<Func<TInterface, bool>> filter)
    {
        var entities = modelBuilder.Model.GetEntityTypes()
            .Where(e => e.BaseType is null && e.ClrType.IsAssignableTo(typeof(TInterface)))
            .Select(e => e.ClrType);

        foreach (var entity in entities)
        {
            var parameterType = Expression.Parameter(modelBuilder.Entity(entity).Metadata.ClrType);
            var filterBody = ReplacingExpressionVisitor.Replace(filter.Parameters.Single(), parameterType, filter.Body);
            modelBuilder.Entity(entity).HasQueryFilter(filterName, Expression.Lambda(filterBody, parameterType));
        }
        
        return modelBuilder;
    }
}