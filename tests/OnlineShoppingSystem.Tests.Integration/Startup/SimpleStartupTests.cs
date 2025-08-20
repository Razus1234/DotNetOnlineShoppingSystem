using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Interfaces;
using OnlineShoppingSystem.Infrastructure.Data;
using System.Net;

namespace OnlineShoppingSystem.Tests.Integration.Startup;

[TestClass]
public class SimpleStartupTests
{
    private static WebApplicationFactory<Program>? _factory;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database for testing
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDatabase");
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
    public async Task Application_Should_Start_Successfully()
    {
        // Arrange & Act
        var client = _factory!.CreateClient();

        // Assert
        var response = await client.GetAsync("/");
        
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("Online Shopping System API is running!"));
    }

    [TestMethod]
    public void Should_Register_All_Core_Services()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert - Core application services
        Assert.IsNotNull(services.GetService<IUserService>());
        Assert.IsNotNull(services.GetService<IProductService>());
        Assert.IsNotNull(services.GetService<ICartService>());
        Assert.IsNotNull(services.GetService<IOrderService>());
        Assert.IsNotNull(services.GetService<IPaymentService>());

        // Infrastructure services
        Assert.IsNotNull(services.GetService<IUnitOfWork>());
        Assert.IsNotNull(services.GetService<IJwtTokenService>());
        Assert.IsNotNull(services.GetService<IPasswordHasher>());
        Assert.IsNotNull(services.GetService<IPaymentGateway>());
        Assert.IsNotNull(services.GetService<ICacheService>());

        // Database context
        Assert.IsNotNull(services.GetService<ApplicationDbContext>());
        Assert.IsNotNull(services.GetService<IApplicationDbContext>());
    }

    [TestMethod]
    public void Should_Register_All_Repositories()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert
        Assert.IsNotNull(services.GetService<IUserRepository>());
        Assert.IsNotNull(services.GetService<IProductRepository>());
        Assert.IsNotNull(services.GetService<ICartRepository>());
        Assert.IsNotNull(services.GetService<IOrderRepository>());
        Assert.IsNotNull(services.GetService<IPaymentRepository>());
    }

    [TestMethod]
    public async Task Health_Endpoints_Should_Be_Available()
    {
        // Arrange
        var client = _factory!.CreateClient();

        // Act & Assert
        var healthResponse = await client.GetAsync("/health");
        Assert.IsTrue(healthResponse.IsSuccessStatusCode || healthResponse.StatusCode == HttpStatusCode.ServiceUnavailable);

        var liveResponse = await client.GetAsync("/health/live");
        Assert.AreEqual(HttpStatusCode.OK, liveResponse.StatusCode);
    }

    [TestMethod]
    public void Should_Have_Authentication_And_Authorization_Services()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert
        var authenticationService = services.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        Assert.IsNotNull(authenticationService);

        var authorizationService = services.GetService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
        Assert.IsNotNull(authorizationService);
    }

    [TestMethod]
    public void Should_Have_AutoMapper_With_Valid_Configuration()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var mapper = services.GetService<AutoMapper.IMapper>();

        // Assert
        Assert.IsNotNull(mapper);
        
        // Verify mapper configuration is valid
        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [TestMethod]
    public void Should_Have_Configuration_Services()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var configuration = services.GetService<Microsoft.Extensions.Configuration.IConfiguration>();

        // Assert
        Assert.IsNotNull(configuration);
        
        // Verify key configuration sections exist
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        Assert.IsNotNull(connectionString);
        Assert.IsTrue(connectionString.Length > 0);

        var jwtSection = configuration.GetSection("JwtSettings");
        Assert.IsNotNull(jwtSection);
        Assert.IsNotNull(jwtSection["SecretKey"]);

        var stripeSection = configuration.GetSection("Stripe");
        Assert.IsNotNull(stripeSection);
        Assert.IsNotNull(stripeSection["SecretKey"]);
    }

    [TestMethod]
    public void Should_Have_Memory_Cache_Service()
    {
        // Arrange
        using var scope = _factory!.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var memoryCache = services.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();

        // Assert
        Assert.IsNotNull(memoryCache);
    }
}