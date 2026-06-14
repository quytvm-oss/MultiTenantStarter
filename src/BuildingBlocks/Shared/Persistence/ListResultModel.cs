namespace Shared.Persistence;

public record ListResultModel<T>(IEnumerable<T> Items, long TotalItems, int? PageIndex, int? PageSize)
    where T : notnull
{
    public static ListResultModel<T> Empty => new(Enumerable.Empty<T>().ToList(), 0, 0, 0);

    public static ListResultModel<T> Create(IEnumerable<T> items, long totalItems = 0, int? page = 1, int? pageSize = 20)
    {
        return new ListResultModel<T>(items, totalItems, page, pageSize);
    }

    public ListResultModel<U> Map<U>(Func<T, U> map)
    {
        return ListResultModel<U>.Create(Items.Select(map).ToList(), TotalItems, PageIndex, PageSize);
    }
}