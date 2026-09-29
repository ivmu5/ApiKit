using System.Text.Json;

namespace ApiKit.Management.Models;

/// <summary>
/// Унифицированная страница CRUD-ресурсов для management plane.
/// </summary>
public sealed record ManagementResourcePage
{
    /// <summary>Элементы страницы в форме read-model конкретного ресурса.</summary>
    public required IReadOnlyList<JsonElement> Items { get; init; }

    /// <summary>Номер страницы, начиная с 1.</summary>
    public required int Page { get; init; }

    /// <summary>Размер страницы.</summary>
    public required int PageSize { get; init; }

    /// <summary>Общее количество элементов.</summary>
    public required long TotalCount { get; init; }

    /// <summary>Общее количество страниц.</summary>
    public long TotalPages =>
        PageSize <= 0
            ? 0
            : TotalCount / PageSize + (TotalCount % PageSize == 0 ? 0 : 1);

    /// <summary>Указывает, существует ли предыдущая страница.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Указывает, существует ли следующая страница.</summary>
    public bool HasNextPage => Page < TotalPages;
}
