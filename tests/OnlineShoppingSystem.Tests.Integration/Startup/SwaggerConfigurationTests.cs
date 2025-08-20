using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineShoppingSystem.Infrastructure.Data;
using System.Net;
using FluentAssertions;

namespace OnlineShoppingSystem.Tests.Integration.Startup;

[TestClass]
public class SwaggerConfigurationTests
{
    private static WebApplicationFactory<Program>? _factory;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development"); // Swagger is enabled in Development
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database for testing
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("SwaggerTestDb");
                    });
                });
            });
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task Swagger_Json_Should_Be_Accessible()
    {
        // Arrange
        var client = _factory!.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Online Shopping System API");
        content.Should().Contain("\"version\": \"v1\"");
    }

    [TestMethod]
    public async Task Swagger_UI_Should_Be_Accessible()
    {
        // Arrange
        var client = _factory!.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Swagger UI");
        content.Should().Contain("Online Shopping System API");
    }

    [TestMethod]
    public async Task Swagger_Should_Include_JWT_Authentication()
    {
        // Arrange
        var client = _factory!.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Bearer");
        content.Should().Contain("securitySchemes");
        content.Should().Contain("Bearer");
    }

    [TestMethod]
    public async Task Custom_CSS_Should_Be_Accessible()
    {
        // Arrange
        var client = _factory!.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger-ui/custom.css");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/css");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("swagger-ui");
        content.Should().Contain("topbar");
    }
}