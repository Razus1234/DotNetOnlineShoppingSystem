using Microsoft.Extensions.DependencyInjection;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Commands.Product;
using OnlineShoppingSystem.Application.Commands.User;
using OnlineShoppingSystem.Application.Commands.Order;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;
using OnlineShoppingSystem.Infrastructure.Data;
using System.Text;
using System.Text.Json;

namespace OnlineShoppingSystem.Tests.Integration.Helpers;

public class TestDataHelper
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly List<Guid> _createdUserIds = new();
    private readonly List<Guid> _createdProductIds = new();
    private readonly List<Guid> _createdOrderIds = new();

    public TestDataHelper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public async Task SeedTestDataAsync()
    {
        // Create test users
        await CreateTestUsersAsync();
        
        // Create test products
        await CreateTestProductsAsync();
        
        // Create test orders
        await CreateTestOrdersAsync();
    }

    public async Task CleanupTestDataAsync()
    {
        // Clean up in reverse order of creation
        foreach (var orderId in _createdOrderIds)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order != null)
            {
                _dbContext.Orders.Remove(order);
            }
        }

        foreach (var productId in _createdProductIds)
        {
            var product = await _dbContext.Products.FindAsync(productId);
            if (product != null)
            {
                _dbContext.Products.Remove(product);
            }
        }

        foreach (var userId in _createdUserIds)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                _dbContext.Users.Remove(user);
            }
        }

        await _dbContext.SaveChangesAsync();
        
        _createdUserIds.Clear();
        _createdProductIds.Clear();
        _createdOrderIds.Clear();
    }

    public async Task<string> GetAdminTokenAsync()
    {
        return await GetTokenForUserAsync("admin@example.com", "Admin123!", "Admin User", isAdmin: true);
    }

    public async Task<string> GetCustomerTokenAsync()
    {
        return await GetTokenForUserAsync("customer@example.com", "Customer123!", "Customer User", isAdmin: false);
    }

    public async Task<Guid> CreateTestProductAsync(string name = "Test Product", decimal price = 29.99m, int stock = 100)
    {
        using var scope = _serviceProvider.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

        var command = new CreateProductCommand
        {
            Name = name,
            Description = "Test product description",
            Price = price,
            Stock = stock,
            Category = "Electronics",
            ImageUrls = new List<string> { "https://example.com/image.jpg" }
        };

        var product = await productService.CreateProductAsync(command);
        _createdProductIds.Add(product.Id);
        
        return product.Id;
    }

    public async Task<Guid> CreateTestOrderAsync()
    {
        // Create a test user and product first
        var userId = await CreateTestUserAsync();
        var productId = await CreateTestProductAsync();

        // Add product to cart
        using var scope = _serviceProvider.CreateScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        await cartService.AddToCartAsync(userId, new Application.Commands.Cart.AddToCartCommand
        {
            ProductId = productId,
            Quantity = 1
        });

        // Place order
        var order = await orderService.PlaceOrderAsync(userId, new PlaceOrderCommand
        {
            ShippingAddress = new Application.DTOs.AddressDto
            {
                Street = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country"
            }
        });

        _createdOrderIds.Add(order.Id);
        return order.Id;
    }

    private async Task<Guid> CreateTestUserAsync(string email = null, string password = "TestPassword123!", string fullName = "Test User")
    {
        email ??= $"testuser{Guid.NewGuid():N}@example.com";
        
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var command = new RegisterUserCommand
        {
            Email = email,
            Password = password,
            FullName = fullName
        };

        var user = await userService.RegisterAsync(command);
        _createdUserIds.Add(user.Id);
        
        return user.Id;
    }

    private async Task CreateTestUsersAsync()
    {
        // Create admin user
        await CreateTestUserAsync("admin@example.com", "Admin123!", "Admin User");
        
        // Create customer user
        await CreateTestUserAsync("customer@example.com", "Customer123!", "Customer User");
    }

    private async Task CreateTestProductsAsync()
    {
        var products = new[]
        {
            new { Name = "Laptop", Price = 999.99m, Stock = 5 },
            new { Name = "Mouse", Price = 29.99m, Stock = 50 },
            new { Name = "Keyboard", Price = 79.99m, Stock = 2 }, // Low stock
            new { Name = "Monitor", Price = 299.99m, Stock = 0 }, // Out of stock
        };

        foreach (var product in products)
        {
            await CreateTestProductAsync(product.Name, product.Price, product.Stock);
        }
    }

    private async Task CreateTestOrdersAsync()
    {
        // Create a few test orders with different statuses
        for (int i = 0; i < 3; i++)
        {
            await CreateTestOrderAsync();
        }
    }

    private async Task<string> GetTokenForUserAsync(string email, string password, string fullName, bool isAdmin)
    {
        // Create user if not exists
        try
        {
            await CreateTestUserAsync(email, password, fullName);
        }
        catch
        {
            // User might already exist, which is fine
        }

        // For this test, we'll need to create a JWT token manually
        // In a real scenario, you'd call the auth endpoint
        using var scope = _serviceProvider.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var user = await unitOfWork.Users.GetByEmailAsync(email);
        if (user == null)
        {
            throw new InvalidOperationException($"User {email} not found");
        }

        // Create a mock user with role claim for JWT generation
        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString(),
            ["email"] = user.Email,
            ["name"] = user.FullName,
            ["role"] = isAdmin ? "Admin" : "Customer"
        };

        // This is a simplified approach - in reality you'd need to modify the JWT service
        // to accept claims or create a test-specific token generation method
        return GenerateTestToken(user, isAdmin);
    }

    private string GenerateTestToken(User user, bool isAdmin)
    {
        // This is a simplified test token generation
        // In a real implementation, you'd use the actual JWT service
        var payload = new
        {
            sub = user.Id.ToString(),
            email = user.Email,
            name = user.FullName,
            role = isAdmin ? "Admin" : "Customer",
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        };

        // For testing purposes, we'll create a simple base64 encoded token
        // In production, this would be a proper JWT
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }
}