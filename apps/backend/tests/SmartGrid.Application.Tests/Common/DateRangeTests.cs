using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Tests.Common;

public class DateRangeTests
{
    [Fact]
    public void Constructor_WithValidRange_SetsProperties()
    {
        var start = new DateOnly(2024, 1, 1);
        var end = new DateOnly(2024, 12, 31);

        var range = new DateRange(start, end);

        Assert.Equal(start, range.Start);
        Assert.Equal(end, range.End);
    }

    [Fact]
    public void Constructor_WithSameStartAndEnd_IsValid()
    {
        var date = new DateOnly(2024, 6, 15);

        var range = new DateRange(date, date);

        Assert.Equal(date, range.Start);
        Assert.Equal(date, range.End);
    }

    [Fact]
    public void Constructor_WithStartAfterEnd_ThrowsArgumentException()
    {
        var start = new DateOnly(2024, 12, 31);
        var end = new DateOnly(2024, 1, 1);

        var ex = Assert.Throws<ArgumentException>(() => new DateRange(start, end));
        Assert.Contains("must not be later than", ex.Message);
    }

    [Fact]
    public void DayCount_CalculatesCorrectInclusiveCount()
    {
        var singleDay = new DateRange(new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 1));
        Assert.Equal(1, singleDay.DayCount);

        var leapYear = new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));
        Assert.Equal(366, leapYear.DayCount);
    }

    [Fact]
    public void TryCreate_WithValidRange_ReturnsTrueAndInstance()
    {
        var start = new DateOnly(2026, 8, 1);
        var end = new DateOnly(2026, 8, 2);

        var success = DateRange.TryCreate(start, end, out var range, out var error);

        Assert.True(success);
        Assert.NotNull(range);
        Assert.Null(error);
        Assert.Equal(start, range.Start);
        Assert.Equal(end, range.End);
        Assert.Equal(2, range.DayCount);
    }

    [Fact]
    public void TryCreate_WithStartAfterEnd_ReturnsFalseAndErrorMessage()
    {
        var start = new DateOnly(2026, 8, 5);
        var end = new DateOnly(2026, 8, 1);

        var success = DateRange.TryCreate(start, end, out var range, out var error);

        Assert.False(success);
        Assert.Null(range);
        Assert.NotNull(error);
        Assert.Contains("must be earlier than or equal to", error);
    }

    [Fact]
    public void TryCreate_WithRangeExceedingMaxDays_ReturnsFalseAndErrorMessage()
    {
        var start = new DateOnly(2025, 1, 1);
        var end = new DateOnly(2026, 1, 3); // 368 days

        var success = DateRange.TryCreate(start, end, out var range, out var error);

        Assert.False(success);
        Assert.Null(range);
        Assert.NotNull(error);
        Assert.Contains("exceeds the maximum allowed limit of 366 days", error);
    }

    [Fact]
    public void Create_WithValidRange_ReturnsInstance()
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 1, 31);

        var range = DateRange.Create(start, end);

        Assert.Equal(start, range.Start);
        Assert.Equal(end, range.End);
        Assert.Equal(31, range.DayCount);
    }

    [Fact]
    public void Create_WithInvalidRange_ThrowsArgumentException()
    {
        var start = new DateOnly(2025, 1, 1);
        var end = new DateOnly(2026, 1, 10);

        Assert.Throws<ArgumentException>(() => DateRange.Create(start, end));
    }
}
