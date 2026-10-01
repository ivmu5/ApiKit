namespace ApiKit.Crud.Models;

/// <summary>
/// Contains one page of query results and its pagination metadata.
/// </summary>
/// <typeparam name="T">The type of each result item.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Initializes a new PagedResult instance.
    /// </summary>
    /// <param name="items">The items value.</param>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The maximum number of items per page.</param>
    /// <param name="totalCount">The total number of available items.</param>
    public PagedResult(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        long totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Номер страницы должен быть не меньше 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Размер страницы должен быть не меньше 1.");
        }

        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), "Общее количество элементов не может быть отрицательным.");
        }

        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets items.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets page.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Gets page size.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets total count.
    /// </summary>
    public long TotalCount { get; }

    /// <summary>
    /// Gets the number of pages needed to include all items.
    /// </summary>
    public long TotalPages =>
        TotalCount / PageSize + (TotalCount % PageSize == 0 ? 0 : 1);

    /// <summary>
    /// Indicates whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Indicates whether a next page exists.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
}
