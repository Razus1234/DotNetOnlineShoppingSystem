using FluentAssertions;
using OnlineShoppingSystem.Application.DTOs;

namespace OnlineShoppingSystem.Tests.Unit.Application.DTOs;

[TestClass]
public class PagedResultDtoTests
{
    [TestMethod]
    public void Should_Calculate_TotalPages_Correctly()
    {
        // Arrange
        var pagedResult = new PagedResultDto<string>
        {
            TotalCount = 25,
            PageSize = 10,
            PageNumber = 1
        };

        // Act & Assert
        pagedResult.TotalPages.Should().Be(3); // 25 / 10 = 2.5, rounded up to 3
    }

    [TestMethod]
    public void Should_Calculate_HasNextPage_Correctly()
    {
        // Arrange
        var pagedResult = new PagedResultDto<string>
        {
            TotalCount = 25,
            PageSize = 10,
            PageNumber = 2
        };

        // Act & Assert
        pagedResult.HasNextPage.Should().BeTrue(); // Page 2 of 3
    }

    [TestMethod]
    public void Should_Calculate_HasPreviousPage_Correctly()
    {
        // Arrange
        var pagedResult = new PagedResultDto<string>
        {
            TotalCount = 25,
            PageSize = 10,
            PageNumber = 2
        };

        // Act & Assert
        pagedResult.HasPreviousPage.Should().BeTrue(); // Page 2, so has previous
    }

    [TestMethod]
    public void Should_Return_False_For_HasNextPage_On_LastPage()
    {
        // Arrange
        var pagedResult = new PagedResultDto<string>
        {
            TotalCount = 25,
            PageSize = 10,
            PageNumber = 3
        };

        // Act & Assert
        pagedResult.HasNextPage.Should().BeFalse(); // Page 3 of 3
    }

    [TestMethod]
    public void Should_Return_False_For_HasPreviousPage_On_FirstPage()
    {
        // Arrange
        var pagedResult = new PagedResultDto<string>
        {
            TotalCount = 25,
            PageSize = 10,
            PageNumber = 1
        };

        // Act & Assert
        pagedResult.HasPreviousPage.Should().BeFalse(); // Page 1
    }

    [TestMethod]
    public void Should_Handle_Empty_Results()
    {
        // Arrange
        var pagedResult = new PagedResultDto<string>
        {
            TotalCount = 0,
            PageSize = 10,
            PageNumber = 1
        };

        // Act & Assert
        pagedResult.TotalPages.Should().Be(0);
        pagedResult.HasNextPage.Should().BeFalse();
        pagedResult.HasPreviousPage.Should().BeFalse();
    }

    [TestMethod]
    public void Should_Handle_Single_Page_Results()
    {
        // Arrange
        var pagedResult = new PagedResultDto<string>
        {
            TotalCount = 5,
            PageSize = 10,
            PageNumber = 1
        };

        // Act & Assert
        pagedResult.TotalPages.Should().Be(1);
        pagedResult.HasNextPage.Should().BeFalse();
        pagedResult.HasPreviousPage.Should().BeFalse();
    }
}