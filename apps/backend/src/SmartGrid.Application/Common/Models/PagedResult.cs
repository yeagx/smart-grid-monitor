namespace SmartGrid.Application.Common.Models;

/// <summary>
/// A paginated result set.
/// </summary>
/// <typeparam name="T">The type of items in this page.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>The items in the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Current page number (1-indexed).</summary>
    public int Page { get; }

    /// <summary>Number of items per page.</summary>
    public int PageSize { get; }

    /// <summary>Total number of records across all pages.</summary>
    public long TotalCount { get; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount / PageSize)
        : 0;

    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>Creates an empty result.</summary>
    public static PagedResult<T> Empty(int page, int pageSize) =>
        new([], page, pageSize, 0);
}
