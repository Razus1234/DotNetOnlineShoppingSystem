using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OnlineShoppingSystem.API;
using System.Security.Claims;

namespace OnlineShoppingSystem.Tests.Unit.API.Security;

[TestClass]
public class AuthorizationPolicyTests
{
    private IServiceProvider _serviceProvider = null!;
    private IAuthorizationService _authorizationService = null!;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Add logging services
        services.AddLogging();
        
        // Configure authorization policies as in Program.cs
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => 
                policy.RequireRole("Admin")
                      .RequireAuthenticatedUser());
                      
            options.AddPolicy("CustomerOnly", policy => 
                policy.RequireRole("Customer")
                      .RequireAuthenticatedUser());
                      
            options.AddPolicy("AdminOrCustomer", policy =>
                policy.RequireRole("Admin", "Customer")
                      .RequireAuthenticatedUser());
                      
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());
        });

        _serviceProvider = services.BuildServiceProvider();
        _authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();
    }

    [TestMethod]
    public async Task AdminOnly_Policy_Should_Allow_Admin_User()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", "Admin");

        // Act
        var result = await _authorizationService.AuthorizeAsync(adminUser, "AdminOnly");

        // Assert
        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task AdminOnly_Policy_Should_Deny_Customer_User()
    {
        // Arrange
        var customerUser = CreateClaimsPrincipal("customer@test.com", "Customer");

        // Act
        var result = await _authorizationService.AuthorizeAsync(customerUser, "AdminOnly");

        // Assert
        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task AdminOnly_Policy_Should_Deny_Unauthenticated_User()
    {
        // Arrange
        var unauthenticatedUser = new ClaimsPrincipal();

        // Act
        var result = await _authorizationService.AuthorizeAsync(unauthenticatedUser, "AdminOnly");

        // Assert
        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task CustomerOnly_Policy_Should_Allow_Customer_User()
    {
        // Arrange
        var customerUser = CreateClaimsPrincipal("customer@test.com", "Customer");

        // Act
        var result = await _authorizationService.AuthorizeAsync(customerUser, "CustomerOnly");

        // Assert
        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task CustomerOnly_Policy_Should_Deny_Admin_User()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", "Admin");

        // Act
        var result = await _authorizationService.AuthorizeAsync(adminUser, "CustomerOnly");

        // Assert
        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task AdminOrCustomer_Policy_Should_Allow_Admin_User()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", "Admin");

        // Act
        var result = await _authorizationService.AuthorizeAsync(adminUser, "AdminOrCustomer");

        // Assert
        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task AdminOrCustomer_Policy_Should_Allow_Customer_User()
    {
        // Arrange
        var customerUser = CreateClaimsPrincipal("customer@test.com", "Customer");

        // Act
        var result = await _authorizationService.AuthorizeAsync(customerUser, "AdminOrCustomer");

        // Assert
        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task AdminOrCustomer_Policy_Should_Deny_Unauthenticated_User()
    {
        // Arrange
        var unauthenticatedUser = new ClaimsPrincipal();

        // Act
        var result = await _authorizationService.AuthorizeAsync(unauthenticatedUser, "AdminOrCustomer");

        // Assert
        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public async Task RequireAuthenticated_Policy_Should_Allow_Any_Authenticated_User()
    {
        // Arrange
        var authenticatedUser = CreateClaimsPrincipal("user@test.com", "SomeRole");

        // Act
        var result = await _authorizationService.AuthorizeAsync(authenticatedUser, "RequireAuthenticated");

        // Assert
        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task RequireAuthenticated_Policy_Should_Deny_Unauthenticated_User()
    {
        // Arrange
        var unauthenticatedUser = new ClaimsPrincipal();

        // Act
        var result = await _authorizationService.AuthorizeAsync(unauthenticatedUser, "RequireAuthenticated");

        // Assert
        Assert.IsFalse(result.Succeeded);
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(string email, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        return new ClaimsPrincipal(identity);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}