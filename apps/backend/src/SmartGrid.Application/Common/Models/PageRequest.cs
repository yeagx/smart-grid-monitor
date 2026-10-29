namespace SmartGrid.Application.Common.Models;

/// <summary>
/// A pagination request. Pages are 1-indexed.
/// </summary>
public sealed class PageRequest
{
    /// <summary>Maximum allowed page size.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Page number (1-indexed).</summary>
    public int Page { get; }

    /// <summary>Number of items per page.</summary>
    public int PageSize { get; }

    public PageRequest(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page,
                "Page number must be at least 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize,
                "Page size must be at least 1.");
        }

        if (pageSize > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize,
                $"Page size must not exceed {MaxPageSize}.");
        }

        Page = page;
        PageSize = pageSize;
    }
}
