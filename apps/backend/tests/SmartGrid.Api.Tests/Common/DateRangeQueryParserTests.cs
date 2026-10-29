using Microsoft.AspNetCore.Http;
using SmartGrid.Api.Common;

namespace SmartGrid.Api.Tests.Common;

public class DateRangeQueryParserTests
{
    private const string TestPath = "/api/dashboard/summary";

    [Fact]
    public void TryParse_WithValidDates_ReturnsTrueAndRange()
    {
        var success = DateRangeQueryParser.TryParse("2026-08-01", "2026-08-02", TestPath, out var period, out var problem);

        Assert.True(success);
        Assert.NotNull(period);
        Assert.Null(problem);
        Assert.Equal(new DateOnly(2026, 8, 1), period.Start);
        Assert.Equal(new DateOnly(2026, 8, 2), period.End);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_WithMissingFrom_ReturnsFalseAndProblemDetails(string? from)
    {
        var success = DateRangeQueryParser.TryParse(from, "2026-08-02", TestPath, out var period, out var problem);

        Assert.False(success);
        Assert.Null(period);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("'from' query parameter is required", problem.Detail);
        Assert.Equal(TestPath, problem.Instance);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026/08/01")]
    [InlineData("01-08-2026")]
    [InlineData("2026-13-01")]
    public void TryParse_WithInvalidFromFormat_ReturnsFalseAndProblemDetails(string from)
    {
        var success = DateRangeQueryParser.TryParse(from, "2026-08-02", TestPath, out var period, out var problem);

        Assert.False(success);
        Assert.Null(period);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("not a valid date", problem.Detail);
        Assert.Contains("Expected format: YYYY-MM-DD", problem.Detail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_WithMissingTo_ReturnsFalseAndProblemDetails(string? to)
    {
        var success = DateRangeQueryParser.TryParse("2026-08-01", to, TestPath, out var period, out var problem);

        Assert.False(success);
        Assert.Null(period);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("'to' query parameter is required", problem.Detail);
        Assert.Equal(TestPath, problem.Instance);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026/08/02")]
    [InlineData("02-08-2026")]
    [InlineData("2026-02-31")]
    public void TryParse_WithInvalidToFormat_ReturnsFalseAndProblemDetails(string to)
    {
        var success = DateRangeQueryParser.TryParse("2026-08-01", to, TestPath, out var period, out var problem);

        Assert.False(success);
        Assert.Null(period);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("not a valid date", problem.Detail);
        Assert.Contains("Expected format: YYYY-MM-DD", problem.Detail);
    }

    [Fact]
    public void TryParse_WithFromAfterTo_ReturnsFalseAndProblemDetails()
    {
        var success = DateRangeQueryParser.TryParse("2026-08-05", "2026-08-01", TestPath, out var period, out var problem);

        Assert.False(success);
        Assert.Null(period);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("must be earlier than or equal to", problem.Detail);
    }

    [Fact]
    public void TryParse_WithRangeExceedingMaxDays_ReturnsFalseAndProblemDetails()
    {
        var success = DateRangeQueryParser.TryParse("2025-01-01", "2026-01-03", TestPath, out var period, out var problem);

        Assert.False(success);
        Assert.Null(period);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("exceeds the maximum allowed limit of 366 days", problem.Detail);
    }
}
