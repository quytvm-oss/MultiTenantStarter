using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore;

using Shared.Persistence;

namespace Persistence.Pagination;

public static class EfCoreQueryableExtensions
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    
    public static Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> source,
        IPagedQuery pagination,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(pagination);

        var pageNumber = pagination.PageNumber is null or <= 0
            ? 1
            : pagination.PageNumber.Value;

        var pageSize = pagination.PageSize is null or <= 0
            ? DefaultPageSize
            : pagination.PageSize.Value;

        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        // Decoupled from specifications: the source is expected to already have any required
        // ordering applied via specifications or explicit ordering at call sites.
        return ToPagedResponseInternalAsync(source, pageNumber, pageSize, cancellationToken);
    }

    private static async Task<PagedResponse<T>> ToPagedResponseInternalAsync<T>(
        IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
        where T : class
    {
        var totalCount = await source.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        if (pageNumber > totalPages && totalPages > 0)
        {
            pageNumber = totalPages;
        }

        var skip = (pageNumber - 1) * pageSize;

        var items = await source
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
    
    public static IQueryable<T> ApplyOrdering<T>(
        this IQueryable<T> source,
        string? orderBy,
        bool descending = false)
    {
        if (string.IsNullOrWhiteSpace(orderBy)) return source;

        var parameter = Expression.Parameter(typeof(T), "x");

        // Support nested property: "Address.City"
        Expression property = parameter;
        foreach (var member in orderBy.Split('.'))
        {
            var propInfo = property.Type.GetProperty(
                member,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (propInfo is null) return source; // property không tồn tại → giữ nguyên thứ tự
            property = Expression.Property(property, propInfo);
        }

        var lambda = Expression.Lambda(property, parameter);

        var methodName = descending
            ? nameof(Queryable.OrderByDescending)
            : nameof(Queryable.OrderBy);

        var method = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.Type);

        return (IQueryable<T>)method.Invoke(null, [source, lambda])!;
    }
    
}