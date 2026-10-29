using SmartGrid.Application.Alerts.Models;
using SmartGrid.Application.Common.Models;
using SmartGrid.Application.GridMap.Queries;

namespace SmartGrid.Application.Tests.GridMap;

public class GridMapCriteriaTests
{
    [Fact]
    public void TryValidate_WithValidCriteria_ReturnsTrue()
    {
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));
        var criteria = new GridMapCriteria
        {
            Period = period,
            District = "Dokki",
            EventType = EventTypes.Theft,
            OnlyWithAlerts = true
        };

        var isValid = criteria.TryValidate(out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("theft")]
    [InlineData("overload")]
    [InlineData("line_drop")]
    [InlineData(null)]
    public void TryValidate_WithSupportedOrNullEventType_ReturnsTrue(string? eventType)
    {
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));
        var criteria = new GridMapCriteria
        {
            Period = period,
            EventType = eventType
        };

        var isValid = criteria.TryValidate(out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("theft_unknown")]
    [InlineData("fire")]
    [InlineData("surge")]
    [InlineData("Theft")] // Case-sensitive check
    public void TryValidate_WithUnsupportedEventType_ReturnsFalseWithError(string invalidType)
    {
        var period = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2));
        var criteria = new GridMapCriteria
        {
            Period = period,
            EventType = invalidType
        };

        var isValid = criteria.TryValidate(out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("Unsupported threat type", error);
    }
}
