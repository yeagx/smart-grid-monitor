using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Tests.Common;

public class PageRequestTests
{
    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    [InlineData(1, PageRequest.MaxPageSize)]
    public void Constructor_WithValidValues_SetsProperties(int page, int pageSize)
    {
        var request = new PageRequest(page, pageSize);

        Assert.Equal(page, request.Page);
        Assert.Equal(pageSize, request.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidPage_ThrowsArgumentOutOfRange(int page)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(page, 10));
        Assert.Equal("page", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Constructor_WithInvalidPageSize_ThrowsArgumentOutOfRange(int pageSize)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(1, pageSize));
        Assert.Equal("pageSize", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithPageSizeExceedingMaximum_ThrowsArgumentOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new PageRequest(1, PageRequest.MaxPageSize + 1));
        Assert.Equal("pageSize", ex.ParamName);
        Assert.Contains($"{PageRequest.MaxPageSize}", ex.Message);
    }

    [Fact]
    public void MaxPageSize_Is100()
    {
        Assert.Equal(100, PageRequest.MaxPageSize);
    }
}
