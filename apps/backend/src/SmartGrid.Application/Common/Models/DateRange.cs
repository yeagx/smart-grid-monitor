using System.Diagnostics.CodeAnalysis;

namespace SmartGrid.Application.Common.Models;

/// <summary>
/// An inclusive date range used for filtering queries.
/// Both <see cref="Start"/> and <see cref="End"/> are inclusive boundaries.
/// </summary>
public sealed class DateRange
{
    /// <summary>Maximum supported duration for date range queries in days (inclusive).</summary>
    public const int MaxSupportedDays = 366;

    /// <summary>Inclusive start date.</summary>
    public DateOnly Start { get; }

    /// <summary>Inclusive end date.</summary>
    public DateOnly End { get; }

    /// <summary>Total number of days in the range (inclusive of both start and end).</summary>
    public int DayCount => End.DayNumber - Start.DayNumber + 1;

    public DateRange(DateOnly start, DateOnly end)
    {
        if (start > end)
        {
            throw new ArgumentException(
                $"Start date ({start:yyyy-MM-dd}) must not be later than end date ({end:yyyy-MM-dd}).",
                nameof(start));
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Attempts to create a <see cref="DateRange"/> verifying start &lt;= end and day count &lt;= maxDays.
    /// </summary>
    public static bool TryCreate(
        DateOnly start,
        DateOnly end,
        [NotNullWhen(true)] out DateRange? range,
        out string? errorMessage,
        int maxDays = MaxSupportedDays)
    {
        if (start > end)
        {
            range = null;
            errorMessage = $"Start date ({start:yyyy-MM-dd}) must be earlier than or equal to end date ({end:yyyy-MM-dd}).";
            return false;
        }

        var days = end.DayNumber - start.DayNumber + 1;
        if (days > maxDays)
        {
            range = null;
            errorMessage = $"The requested date range ({days} days) exceeds the maximum allowed limit of {maxDays} days.";
            return false;
        }

        range = new DateRange(start, end);
        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Creates a <see cref="DateRange"/> enforcing start &lt;= end and day count &lt;= maxDays.
    /// </summary>
    public static DateRange Create(DateOnly start, DateOnly end, int maxDays = MaxSupportedDays)
    {
        if (!TryCreate(start, end, out var range, out var error, maxDays))
        {
            throw new ArgumentException(error);
        }

        return range!;
    }
}
