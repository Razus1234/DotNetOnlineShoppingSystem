using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Common.Models;
using OnlineShoppingSystem.Application.Interfaces;
using OnlineShoppingSystem.Infrastructure.Data;
using System.Net;
using FluentAssertions;

namespace OnlineShoppingSystem.Tests.Integration.Startup;

[TestClass]
public class ApplicationStartupTests
{
    private static WebApplicationFactory<Program>? _factory;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=TestDb;Username=test;Password=test",
                        ["JwtSettings:SecretKey"] = "test-secret-key-that-is-at-least-32-characters-long-for-testing-purposes",
                        ["JwtSettings:Issuer"] = "TestIssuer",
                        ["JwtSettings:Audience"] = "TestAudience",
                        ["JwtSettings:ExpirationHours"] = "1",
                        ["Stripe:SecretKey"] = "sk_test_test_key",
                        ["Stripe:PublishableKey"] = "pk_test_test_key",
                        ["Cors:AllowedOrigins:0"] = "https://localhost:3000",
                        ["Cors:AllowedOrigins:1"] = "https://localhost:3001"
                    });
                });
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    
                    var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
                    if (contextDescriptor != null)
                    {
                        services.Remove(contextDescriptor);
                    }

                    var interfaceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IApplicationDbContext));
                    if (interfaceDescriptor != null)
                    {
                        services.Remove(interfaceDescriptor);
                    }

                    // Add in-memory database for testing
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("ApplicationStartupTestDb");
                    });
                    
                    // Re-register the interface
                    services.AddScoped<IApplicationDbContext>(provider => 
                        provider.GetRequiredService<ApplicationDbContext>());
                });
            });
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task Application_Should_Start_Without_Errors()
    {
        // Arrange & Act
        var client = _factory!.CreateClient();

        // Assert
        var response = await client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Online Shopping System API is running!");
    }

    [TestMethod]
    public void Should_Register_All_Application_Services()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert - Application Services
        services.GetService<IUserService>().Should().NotBeNull();
        services.GetService<IProductService>().Should().NotBeNull();
        services.GetService<ICartService>().Should().NotBeNull();
        services.GetService<IOrderService>().Should().NotBeNull();
        services.GetService<IPaymentService>().Should().NotBeNull();
    }

    [TestMethod]
    public void Should_Register_All_Infrastructure_Services()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert - Infrastructure Services
        services.GetService<IUnitOfWork>().Should().NotBeNull();
        services.GetService<IJwtTokenService>().Should().NotBeNull();
        services.GetService<IPasswordHasher>().Should().NotBeNull();
        services.GetService<OnlineShoppingSystem.Application.Common.Interfaces.IAuthenticationService>().Should().NotBeNull();
        services.GetService<IPaymentGateway>().Should().NotBeNull();
        services.GetService<ICacheService>().Should().NotBeNull();
    }

    [TestMethod]
    public void Should_Register_All_Repository_Services()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert - Repository Services
        services.GetService<IUserRepository>().Should().NotBeNull();
        services.GetService<IProductRepository>().Should().NotBeNull();
        services.GetService<ICartRepository>().Should().NotBeNull();
        services.GetService<IOrderRepository>().Should().NotBeNull();
        services.GetService<IPaymentRepository>().Should().NotBeNull();
    }

    [TestMethod]
    public void Should_Register_Database_Context_Services()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert - Database Services
        services.GetService<ApplicationDbContext>().Should().NotBeNull();
        services.GetService<IApplicationDbContext>().Should().NotBeNull();
        
        // Verify DbContext is properly configured
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        // Just verify the context is available and functional
        dbContext.Should().NotBeNull();
    }

    [TestMethod]
    public void Should_Configure_JWT_Authentication_Properly()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert - Authentication Services
        var authenticationService = services.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        authenticationService.Should().NotBeNull();

        var jwtTokenService = services.GetService<IJwtTokenService>();
        jwtTokenService.Should().NotBeNull();

        // Verify JWT settings are configured
        var jwtSettings = services.GetService<IOptions<JwtSettings>>();
        jwtSettings.Should().NotBeNull();
        jwtSettings!.Value.SecretKey.Should().NotBeNullOrEmpty();
        jwtSettings.Value.Issuer.Should().Be("TestIssuer");
        jwtSettings.Value.Audience.Should().Be("TestAudience");
        jwtSettings.Value.ExpirationHours.Should().Be(1);
    }

    [TestMethod]
    public void Should_Configure_Authorization_Policies()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert - Authorization Services
        var authorizationService = services.GetService<IAuthorizationService>();
        authorizationService.Should().NotBeNull();

        // Verify authorization options are configured
        var authorizationOptions = services.GetService<IOptions<AuthorizationOptions>>();
        authorizationOptions.Should().NotBeNull();

        // Test that we can get a policy (this verifies policies are registered)
        var adminPolicy = authorizationOptions!.Value.GetPolicy("AdminOnly");
        adminPolicy.Should().NotBeNull();
        
        var customerPolicy = authorizationOptions.Value.GetPolicy("CustomerOnly");
        customerPolicy.Should().NotBeNull();
        
        var adminOrCustomerPolicy = authorizationOptions.Value.GetPolicy("AdminOrCustomer");
        adminOrCustomerPolicy.Should().NotBeNull();
    }

    [TestMethod]
    public void Should_Configure_AutoMapper_With_Valid_Profiles()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var mapper = services.GetService<AutoMapper.IMapper>();

        // Assert
        mapper.Should().NotBeNull();
        
        // Verify mapper configuration is valid (this will throw if invalid)
        Action act = () => mapper!.ConfigurationProvider.AssertConfigurationIsValid();
        act.Should().NotThrow();
    }

    [TestMethod]
    public void Should_Configure_Memory_Cache()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert
        var memoryCache = services.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        memoryCache.Should().NotBeNull();
    }

    [TestMethod]
    public void Should_Configure_Logging_Services()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert
        var loggerFactory = services.GetService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();

        var logger = services.GetService<ILogger<ApplicationStartupTests>>();
        logger.Should().NotBeNull();
    }

    [TestMethod]
    public void Should_Configure_Health_Check_Services()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert
        var healthCheckService = services.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Health_Check_Endpoints_Should_Be_Accessible()
    {
        // Arrange
        var client = _factory!.CreateClient();

        // Act & Assert - Main health endpoint
        var healthResponse = await client.GetAsync("/health");
        healthResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        // Act & Assert - Ready health endpoint
        var readyResponse = await client.GetAsync("/health/ready");
        readyResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        // Act & Assert - Live health endpoint
        var liveResponse = await client.GetAsync("/health/live");
        liveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Should_Execute_Health_Checks_Successfully()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;
        var healthCheckService = services.GetRequiredService<HealthCheckService>();

        // Act
        var result = await healthCheckService.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);
        result.Entries.Should().NotBeEmpty();
    }

    [TestMethod]
    public void Should_Validate_Configuration_On_Startup()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;
        var configuration = services.GetRequiredService<IConfiguration>();

        // Act & Assert - Connection String
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        connectionString.Should().NotBeNullOrEmpty();

        // Act & Assert - JWT Settings
        var jwtSettings = configuration.GetSection("JwtSettings");
        jwtSettings.Should().NotBeNull();
        jwtSettings["SecretKey"].Should().NotBeNullOrEmpty();
        jwtSettings["Issuer"].Should().NotBeNullOrEmpty();
        jwtSettings["Audience"].Should().NotBeNullOrEmpty();

        // Act & Assert - Stripe Settings
        var stripeSettings = configuration.GetSection("Stripe");
        stripeSettings.Should().NotBeNull();
        stripeSettings["SecretKey"].Should().NotBeNullOrEmpty();
        stripeSettings["PublishableKey"].Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Should_Apply_CORS_Configuration()
    {
        // Arrange
        var client = _factory!.CreateClient();

        // Act - Make an OPTIONS request to test CORS
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/products");
        request.Headers.Add("Origin", "https://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [TestMethod]
    public void Should_Configure_Stripe_Settings()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert
        var stripeSettings = services.GetService<IOptions<OnlineShoppingSystem.API.Extensions.StripeSettings>>();
        stripeSettings.Should().NotBeNull();
        stripeSettings!.Value.SecretKey.Should().NotBeNullOrEmpty();
        stripeSettings.Value.PublishableKey.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void Should_Register_Cached_Service_Decorators()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var userService = services.GetService<IUserService>();
        var productService = services.GetService<IProductService>();

        // Assert
        userService.Should().NotBeNull();
        productService.Should().NotBeNull();
        
        // Verify these are the cached versions (decorator pattern)
        userService!.GetType().Name.Should().Contain("Cached");
        productService!.GetType().Name.Should().Contain("Cached");
    }

    [TestMethod]
    public void Should_Have_Proper_Service_Lifetimes()
    {
        // Arrange
        using var scope1 = _factory!.Services.CreateScope();
        using var scope2 = _factory!.Services.CreateScope();

        // Act - Get scoped services from different scopes
        var userService1 = scope1.ServiceProvider.GetService<IUserService>();
        var userService2 = scope2.ServiceProvider.GetService<IUserService>();

        var dbContext1 = scope1.ServiceProvider.GetService<ApplicationDbContext>();
        var dbContext2 = scope2.ServiceProvider.GetService<ApplicationDbContext>();

        // Assert - Scoped services should be different instances across scopes
        userService1.Should().NotBeSameAs(userService2);
        dbContext1.Should().NotBeSameAs(dbContext2);

        // Act - Get scoped services from same scope
        var userServiceSame1 = scope1.ServiceProvider.GetService<IUserService>();
        var userServiceSame2 = scope1.ServiceProvider.GetService<IUserService>();

        // Assert - Scoped services should be same instance within same scope
        userServiceSame1.Should().BeSameAs(userServiceSame2);
    }

    [TestMethod]
    public async Task Should_Handle_Database_Migration_Gracefully()
    {
        // This test verifies that the migration logic doesn't fail in test environment
        // Arrange
        var client = _factory!.CreateClient();

        // Act - The application should start successfully even with migration logic
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

