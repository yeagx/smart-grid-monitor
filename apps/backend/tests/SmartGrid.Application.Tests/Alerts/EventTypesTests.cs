using SmartGrid.Application.Alerts.Models;

namespace SmartGrid.Application.Tests.Alerts;

public class EventTypesTests
{
    [Theory]
    [InlineData("theft")]
    [InlineData("overload")]
    [InlineData("line_drop")]
    public void IsValid_WithKnownType_ReturnsTrue(string eventType)
    {
        Assert.True(EventTypes.IsValid(eventType));
    }

    [Theory]
    [InlineData("Theft")]
    [InlineData("OVERLOAD")]
    [InlineData("line-drop")]
    [InlineData("unknown")]
    [InlineData("")]
    public void IsValid_WithUnknownType_ReturnsFalse(string eventType)
    {
        Assert.False(EventTypes.IsValid(eventType));
    }

    [Fact]
    public void IsValid_WithNull_ReturnsFalse()
    {
        Assert.False(EventTypes.IsValid(null));
    }

    [Fact]
    public void All_ContainsExactlyThreeTypes()
    {
        Assert.Equal(3, EventTypes.All.Count);
        Assert.Contains("theft", EventTypes.All);
        Assert.Contains("overload", EventTypes.All);
        Assert.Contains("line_drop", EventTypes.All);
    }
}
