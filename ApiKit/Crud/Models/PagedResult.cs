namespace ApiKit.Crud.Models;

/// <summary>
/// Представляет страницу результатов CRUD-запроса.
/// </summary>
/// <typeparam name="T">Тип возвращаемой модели.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Создаёт описание страницы результатов.
    /// </summary>
    /// <param name="items">Элементы текущей страницы.</param>
    /// <param name="page">Номер текущей страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="totalCount">Общее количество элементов после фильтрации.</param>
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
    /// Элементы текущей страницы.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Номер текущей страницы.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Размер страницы.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Общее количество элементов после фильтрации.
    /// </summary>
    public long TotalCount { get; }

    /// <summary>
    /// Общее количество страниц.
    /// </summary>
    public long TotalPages =>
        TotalCount / PageSize + (TotalCount % PageSize == 0 ? 0 : 1);

    /// <summary>
    /// Указывает, существует ли предыдущая страница.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Указывает, существует ли следующая страница.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
}
