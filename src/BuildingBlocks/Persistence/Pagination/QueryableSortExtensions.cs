using System.Linq.Expressions;

namespace Persistence.Pagination;

public static class QueryableSortExtensions
{
    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        string? sort,
        IReadOnlyDictionary<string, Expression<Func<T, object?>>> sortableFields,
        params Expression<Func<T, object?>>[] defaultSorts)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(sortableFields);
        ArgumentNullException.ThrowIfNull(defaultSorts);

        if (string.IsNullOrWhiteSpace(sort))
        {
            return ApplyDefaultSorting(query, defaultSorts);
        }

        var sortParts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var part in sortParts)
        {
            var (field, descending) = ParseSortField(part);

            if (string.IsNullOrWhiteSpace(field))
            {
                continue;
            }

            if (!sortableFields.TryGetValue(field, out var selector))
            {
                continue;
            }

            orderedQuery = ApplySortExpression(
                query,
                orderedQuery,
                selector,
                descending);
        }

        return orderedQuery
               ?? ApplyDefaultSorting(query, defaultSorts);
    }
    
    private static IQueryable<T> ApplyDefaultSorting<T>(
        IQueryable<T> query,
        IReadOnlyList<Expression<Func<T, object?>>> defaultSorts)
    {
        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var selector in defaultSorts)
        {
            orderedQuery = ApplySortExpression(
                query,
                orderedQuery,
                selector,
                descending: false);
        }

        return orderedQuery ?? query;
    }
    
    private static IOrderedQueryable<T> ApplySortExpression<T>(
        IQueryable<T> query,
        IOrderedQueryable<T>? orderedQuery,
        Expression<Func<T, object?>> selector,
        bool descending)
    {
        if (orderedQuery is null)
        {
            return descending
                ? query.OrderByDescending(selector)
                : query.OrderBy(selector);
        }

        return descending
            ? orderedQuery.ThenByDescending(selector)
            : orderedQuery.ThenBy(selector);
    }

    private static (string field, bool descending) ParseSortField(string part)
    {
        var descending = part.StartsWith('-');
        var field = descending ? part[1..] : part;
        return (field, descending);
    }
}