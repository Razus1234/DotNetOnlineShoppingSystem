using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Text.Json;

namespace OnlineShoppingSystem.Tests.Integration.HealthChecks;

[TestClass]
public class HealthCheckIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task HealthCheck_Endpoint_ShouldReturnHealthStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(string.IsNullOrEmpty(content));
        
        // Verify it's valid JSON
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.IsTrue(healthReport.TryGetProperty("status", out _));
    }

    [TestMethod]
    public async Task HealthCheck_Ready_Endpoint_ShouldReturnReadyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        // Should return OK even if no checks are tagged as "ready"
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [TestMethod]
    public async Task HealthCheck_Live_Endpoint_ShouldReturnLiveStatus()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task HealthCheck_ShouldIncludeAllRegisteredChecks()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var healthReport = await healthCheckService.CheckHealthAsync();

        // Assert
        Assert.IsNotNull(healthReport);
        Assert.IsTrue(healthReport.Entries.Count > 0);
        
        // Verify specific health checks are registered
        Assert.IsTrue(healthReport.Entries.ContainsKey("database"));
        Assert.IsTrue(healthReport.Entries.ContainsKey("postgresql"));
        Assert.IsTrue(healthReport.Entries.ContainsKey("database-custom"));
        Assert.IsTrue(healthReport.Entries.ContainsKey("payment-gateway"));
        Assert.IsTrue(healthReport.Entries.ContainsKey("memory"));
    }

    [TestMethod]
    public async Task HealthCheck_DatabaseCheck_ShouldReportStatus()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var healthReport = await healthCheckService.CheckHealthAsync();

        // Assert
        Assert.IsTrue(healthReport.Entries.ContainsKey("database-custom"));
        var databaseCheck = healthReport.Entries["database-custom"];
        
        // Database check should be either healthy or unhealthy (not degraded)
        Assert.IsTrue(databaseCheck.Status == HealthStatus.Healthy || databaseCheck.Status == HealthStatus.Unhealthy);
    }

    [TestMethod]
    public async Task HealthCheck_PaymentGatewayCheck_ShouldReportStatus()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var healthReport = await healthCheckService.CheckHealthAsync();

        // Assert
        Assert.IsTrue(healthReport.Entries.ContainsKey("payment-gateway"));
        var paymentGatewayCheck = healthReport.Entries["payment-gateway"];
        
        // Payment gateway check can be healthy, degraded, or unhealthy
        Assert.IsTrue(paymentGatewayCheck.Status == HealthStatus.Healthy || 
                     paymentGatewayCheck.Status == HealthStatus.Degraded || 
                     paymentGatewayCheck.Status == HealthStatus.Unhealthy);
    }

    [TestMethod]
    public async Task HealthCheck_MemoryCheck_ShouldReportMemoryUsage()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var healthReport = await healthCheckService.CheckHealthAsync();

        // Assert
        Assert.IsTrue(healthReport.Entries.ContainsKey("memory"));
        var memoryCheck = healthReport.Entries["memory"];
        
        Assert.IsNotNull(memoryCheck.Data);
        Assert.IsTrue(memoryCheck.Data.ContainsKey("AllocatedBytes"));
        Assert.IsTrue(memoryCheck.Data.ContainsKey("Gen0Collections"));
        Assert.IsTrue(memoryCheck.Data.ContainsKey("Gen1Collections"));
        Assert.IsTrue(memoryCheck.Data.ContainsKey("Gen2Collections"));
    }
}