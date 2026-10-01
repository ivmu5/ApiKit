using System.Text.Json;

namespace ApiKit.Management.Models;

/// <summary>
/// Represents a page of resources returned by management CRUD.
/// </summary>
public sealed record ManagementResourcePage
{
    /// <summary>
    /// Gets or sets items.
    /// </summary>
    public required IReadOnlyList<JsonElement> Items { get; init; }

    /// <summary>
    /// Gets or sets page.
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Gets or sets page size.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Gets or sets total count.
    /// </summary>
    public required long TotalCount { get; init; }

    /// <summary>
    /// Gets or sets total pages.
    /// </summary>
    public long TotalPages =>
        PageSize <= 0
            ? 0
            : TotalCount / PageSize + (TotalCount % PageSize == 0 ? 0 : 1);

    /// <summary>
    /// Gets or sets whether has previous page is enabled.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets or sets whether has next page is enabled.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
}
