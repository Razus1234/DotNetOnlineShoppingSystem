using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Domain.Enums;
using System.Net;
using System.Net.Http.Json;

namespace OnlineShoppingSystem.Tests.Integration.Controllers;

[TestClass]
public class AdminControllerTests : BaseControllerTest
{
    // Remove the problematic setup method for now

    [TestMethod]
    public async Task GetDashboard_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/admin/dashboard");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetDashboard_WithCustomerRole_ReturnsForbidden()
    {
        // Arrange
        var customerToken = await GetJwtTokenAsync("customer@example.com", "Customer123!");
        SetAuthorizationHeader(customerToken);

        // Act
        var response = await Client.GetAsync("/api/admin/dashboard");

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task GetDashboard_WithAdminRole_ReturnsSuccess()
    {
        // Arrange
        var adminToken = await GetJwtTokenAsync("admin@example.com", "Admin123!");
        SetAuthorizationHeader(adminToken);

        // Act
        var response = await Client.GetAsync("/api/admin/dashboard");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var dashboard = await DeserializeResponseAsync<AdminDashboardDto>(response);

        Assert.IsNotNull(dashboard);
        Assert.IsTrue(dashboard.TotalOrders >= 0);
        Assert.IsTrue(dashboard.PendingOrders >= 0);
        Assert.IsTrue(dashboard.LowStockProducts >= 0);
        Assert.IsNotNull(dashboard.RecentOrders);
        Assert.IsNotNull(dashboard.LowStockItems);
    }

    [TestMethod]
    public async Task GetAllOrders_WithAdminRole_ReturnsPagedResults()
    {
        // Arrange
        var adminToken = await GetJwtTokenAsync("admin@example.com", "Admin123!");
        SetAuthorizationHeader(adminToken);

        // Act
        var response = await Client.GetAsync("/api/admin/orders?pageNumber=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var pagedResult = await DeserializeResponseAsync<PagedResultDto<OrderDto>>(response);

        Assert.IsNotNull(pagedResult);
        Assert.IsNotNull(pagedResult.Items);
        Assert.IsTrue(pagedResult.TotalCount >= 0);
        Assert.AreEqual(1, pagedResult.PageNumber);
        Assert.AreEqual(10, pagedResult.PageSize);
    }

    [TestMethod]
    public async Task UpdateOrderStatus_WithInvalidOrderId_ReturnsNotFound()
    {
        // Arrange
        var adminToken = await GetJwtTokenAsync("admin@example.com", "Admin123!");
        SetAuthorizationHeader(adminToken);

        var orderId = Guid.NewGuid(); // Non-existent order
        var updateRequest = new { Status = OrderStatus.Processing };

        // Act
        var response = await Client.PatchAsync($"/api/admin/orders/{orderId}/status", 
            JsonContent.Create(updateRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateOrderStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var adminToken = await GetJwtTokenAsync("admin@example.com", "Admin123!");
        SetAuthorizationHeader(adminToken);

        var orderId = Guid.NewGuid();
        var updateRequest = new { Status = 999 }; // Invalid status

        // Act
        var response = await Client.PatchAsync($"/api/admin/orders/{orderId}/status", 
            JsonContent.Create(updateRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task GetSalesReport_WithValidDateRange_ReturnsReport()
    {
        // Arrange
        var adminToken = await GetJwtTokenAsync("admin@example.com", "Admin123!");
        SetAuthorizationHeader(adminToken);

        var startDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/admin/reports/sales?startDate={startDate}&endDate={endDate}");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var report = await DeserializeResponseAsync<SalesReportDto>(response);

        Assert.IsNotNull(report);
        Assert.IsTrue(report.TotalRevenue >= 0);
        Assert.IsTrue(report.TotalOrders >= 0);
        Assert.IsTrue(report.AverageOrderValue >= 0);
        Assert.IsNotNull(report.DailySales);
        Assert.IsNotNull(report.TopSellingProducts);
    }

    [TestMethod]
    public async Task GetSalesReport_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var adminToken = await GetJwtTokenAsync("admin@example.com", "Admin123!");
        SetAuthorizationHeader(adminToken);

        var startDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"); // End before start

        // Act
        var response = await Client.GetAsync($"/api/admin/reports/sales?startDate={startDate}&endDate={endDate}");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task GetInventoryReport_WithAdminRole_ReturnsReport()
    {
        // Arrange
        var adminToken = await GetJwtTokenAsync("admin@example.com", "Admin123!");
        SetAuthorizationHeader(adminToken);

        // Act
        var response = await Client.GetAsync("/api/admin/reports/inventory?lowStockThreshold=10");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var report = await DeserializeResponseAsync<InventoryReportDto>(response);

        Assert.IsNotNull(report);
        Assert.IsTrue(report.TotalProducts >= 0);
        Assert.IsTrue(report.OutOfStockProducts >= 0);
        Assert.IsTrue(report.LowStockProducts >= 0);
        Assert.IsTrue(report.TotalStockValue >= 0);
        Assert.AreEqual(10, report.LowStockThreshold);
        Assert.IsNotNull(report.LowStockItems);
        Assert.IsNotNull(report.CategoryBreakdown);
    }

    [TestMethod]
    public async Task BulkUpdateStock_WithNegativeStock_ReturnsPartialFailure()
    {
        // Arrange
        var adminToken = await GetJwtTokenAsync("admin@example.com", "Admin123!");
        SetAuthorizationHeader(adminToken);

        var productId = Guid.NewGuid(); // Non-existent product
        var updateRequest = new
        {
            Updates = new[]
            {
                new { ProductId = productId, NewStock = -10 } // Invalid negative stock
            }
        };

        // Act
        var response = await Client.PatchAsync("/api/admin/inventory/bulk-update", 
            JsonContent.Create(updateRequest));

        // Assert
        response.EnsureSuccessStatusCode();
        
        var result = await DeserializeResponseAsync<BulkStockUpdateResultDto>(response);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.TotalUpdates);
        Assert.AreEqual(0, result.SuccessfulUpdates);
        Assert.AreEqual(1, result.FailedUpdates);
        Assert.IsNotNull(result.Results);
        Assert.AreEqual(1, result.Results.Count);
        Assert.IsFalse(result.Results.First().Success);
        Assert.IsNotNull(result.Results.First().ErrorMessage);
    }

    [TestMethod]
    public async Task BulkUpdateStock_WithEmptyUpdates_ReturnsBadRequest()
    {
        // Arrange
        var adminToken = await GetJwtTokenAsync("admin@example.com", "Admin123!");
        SetAuthorizationHeader(adminToken);

        var updateRequest = new
        {
            Updates = Array.Empty<object>()
        };

        // Act
        var response = await Client.PatchAsync("/api/admin/inventory/bulk-update", 
            JsonContent.Create(updateRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task AdminEndpoints_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        var customerToken = await GetJwtTokenAsync("customer@example.com", "Customer123!");
        SetAuthorizationHeader(customerToken);

        var endpoints = new[]
        {
            "/api/admin/dashboard",
            "/api/admin/orders",
            "/api/admin/reports/sales",
            "/api/admin/reports/inventory"
        };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await Client.GetAsync(endpoint);

            // Assert
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, 
                $"Endpoint {endpoint} should return Forbidden for customer role");
        }
    }
}