using SmartGrid.Application.Common.Models;

namespace SmartGrid.Application.Tests.Common;

public class PagedResultTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var items = new List<string> { "a", "b", "c" };

        var result = new PagedResult<string>(items, page: 2, pageSize: 10, totalCount: 25);

        Assert.Equal(items, result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public void TotalPages_CalculatesCorrectly()
    {
        var result = new PagedResult<int>([], page: 1, pageSize: 10, totalCount: 25);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WhenExactlyDivisible_IsCorrect()
    {
        var result = new PagedResult<int>([], page: 1, pageSize: 10, totalCount: 30);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WhenZeroCount_IsZero()
    {
        var result = new PagedResult<int>([], page: 1, pageSize: 10, totalCount: 0);

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void Empty_ReturnsEmptyResult()
    {
        var result = PagedResult<string>.Empty(page: 1, pageSize: 20);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void Constructor_WithNullItems_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PagedResult<string>(null!, page: 1, pageSize: 10, totalCount: 0));
    }
}
