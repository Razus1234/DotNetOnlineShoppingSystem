# Design Document

## Overview

The Online Shopping System follows Clean Architecture principles with a clear separation of concerns across multiple layers. The system is built using .NET 8 Web API with PostgreSQL as the database, implementing the Repository + Unit of Work pattern for data access. The architecture ensures maintainability, testability, and scalability while supporting JWT-based authentication and integration with external payment gateways.

## Architecture

### Layer Structure

```
┌─────────────────────────────────────────┐
│           Presentation Layer            │
│  (Controllers, DTOs, Middleware)        │
├─────────────────────────────────────────┤
│           Application Layer             │
│  (Services, Interfaces, Commands)       │
├─────────────────────────────────────────┤
│             Domain Layer                │
│  (Entities, Value Objects, Events)      │
├─────────────────────────────────────────┤
│          Infrastructure Layer           │
│  (Repositories, EF Context, External)   │
└─────────────────────────────────────────┘
```

### Project Structure

- **OnlineShoppingSystem.API** - Web API controllers, middleware, configuration
- **OnlineShoppingSystem.Application** - Business logic, services, DTOs, interfaces
- **OnlineShoppingSystem.Domain** - Core entities, value objects, domain events
- **OnlineShoppingSystem.Infrastructure** - Data access, external services, repositories
- **OnlineShoppingSystem.Tests** - Unit and integration tests

## Components and Interfaces

### Domain Layer

#### Core Entities

```csharp
// User aggregate root
public class User : BaseEntity
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public List<Address> Addresses { get; private set; }
    public Cart Cart { get; private set; }
}

// Product aggregate root
public class Product : BaseEntity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string Category { get; private set; }
    public List<ProductImage> Images { get; private set; }
}

// Order aggregate root
public class Order : BaseEntity
{
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public List<OrderItem> Items { get; private set; }
    public Payment Payment { get; private set; }
}
```

#### Value Objects

```csharp
public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }
}

public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
}
```

### Application Layer

#### Service Interfaces

```csharp
public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterUserCommand command);
    Task<AuthTokenDto> LoginAsync(LoginCommand command);
    Task<UserDto> GetUserProfileAsync(Guid userId);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateUserCommand command);
}

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(ProductQuery query);
    Task<ProductDto> GetProductByIdAsync(Guid productId);
    Task<ProductDto> CreateProductAsync(CreateProductCommand command);
    Task<ProductDto> UpdateProductAsync(Guid productId, UpdateProductCommand command);
    Task DeleteProductAsync(Guid productId);
}

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId);
    Task<CartDto> AddToCartAsync(Guid userId, AddToCartCommand command);
    Task<CartDto> UpdateCartItemAsync(Guid userId, UpdateCartItemCommand command);
    Task RemoveFromCartAsync(Guid userId, Guid productId);
    Task ClearCartAsync(Guid userId);
}

public interface IOrderService
{
    Task<OrderDto> PlaceOrderAsync(Guid userId, PlaceOrderCommand command);
    Task<PagedResult<OrderDto>> GetOrderHistoryAsync(Guid userId, OrderQuery query);
    Task<OrderDto> GetOrderByIdAsync(Guid orderId);
    Task<OrderDto> CancelOrderAsync(Guid orderId);
    Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
}

public interface IPaymentService
{
    Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentCommand command);
    Task<PaymentDto> GetPaymentByOrderIdAsync(Guid orderId);
}
```

### Infrastructure Layer

#### Repository Pattern

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}

public interface IUserRepository : IRepository<User>
{
    Task<User> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
}

public interface IProductRepository : IRepository<Product>
{
    Task<PagedResult<Product>> GetPagedAsync(ProductQuery query);
    Task<IEnumerable<Product>> SearchAsync(string keyword);
    Task<IEnumerable<Product>> GetByCategoryAsync(string category);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<PagedResult<Order>> GetByUserIdAsync(Guid userId, OrderQuery query);
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
}
```

#### Unit of Work Pattern

```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IProductRepository Products { get; }
    ICartRepository Carts { get; }
    IOrderRepository Orders { get; }
    IPaymentRepository Payments { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

## Data Models

### Database Schema Design

The PostgreSQL database schema follows the provided specification with additional considerations for performance and data integrity:

#### Indexing Strategy

```sql
-- Performance indexes
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_products_category ON products(category);
CREATE INDEX idx_products_name_search ON products USING gin(to_tsvector('english', name || ' ' || description));
CREATE INDEX idx_orders_user_id ON orders(user_id);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_orders_created_at ON orders(created_at);
CREATE INDEX idx_cart_items_cart_id ON cart_items(cart_id);
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
```

#### Entity Framework Configuration

```csharp
public class ShoppingDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entity relationships and constraints
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShoppingDbContext).Assembly);
    }
}
```

### DTOs and Mapping

```csharp
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; }
    public List<string> ImageUrls { get; set; }
}

public class CartDto
{
    public Guid Id { get; set; }
    public List<CartItemDto> Items { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; }
    public PaymentDto Payment { get; set; }
}
```

## Error Handling

### Exception Hierarchy

```csharp
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid userId) 
        : base($"User with ID {userId} was not found") { }
}

public class ProductOutOfStockException : DomainException
{
    public ProductOutOfStockException(string productName) 
        : base($"Product '{productName}' is out of stock") { }
}

public class PaymentFailedException : DomainException
{
    public PaymentFailedException(string reason) 
        : base($"Payment failed: {reason}") { }
}
```

### Global Exception Middleware

```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            DomainException => new { error = exception.Message, statusCode = 400 },
            UnauthorizedAccessException => new { error = "Unauthorized", statusCode = 401 },
            _ => new { error = "Internal server error", statusCode = 500 }
        };

        context.Response.StatusCode = response.statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

## Authentication and Authorization

### JWT Configuration

```csharp
public class JwtSettings
{
    public string SecretKey { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int ExpirationHours { get; set; } = 1;
}

public interface IJwtTokenService
{
    string GenerateToken(User user);
    ClaimsPrincipal ValidateToken(string token);
}
```

### Authorization Policies

```csharp
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string CustomerOnly = "CustomerOnly";
}

// In Startup.cs
services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, policy => 
        policy.RequireClaim("role", "Admin"));
    options.AddPolicy(Policies.CustomerOnly, policy => 
        policy.RequireClaim("role", "Customer"));
});
```

## External Integrations

### Payment Gateway (Stripe)

```csharp
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount);
    Task<PaymentStatus> GetPaymentStatusAsync(string transactionId);
}

public class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeClient _stripeClient;
    
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        var paymentIntentService = new PaymentIntentService(_stripeClient);
        var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount = (long)(request.Amount * 100), // Convert to cents
            Currency = "usd",
            PaymentMethodTypes = new List<string> { "card" }
        });
        
        return new PaymentResult
        {
            TransactionId = paymentIntent.Id,
            Status = MapStripeStatus(paymentIntent.Status),
            Amount = request.Amount
        };
    }
}
```

## Testing Strategy

### Unit Testing Approach

```csharp
[TestClass]
public class UserServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IJwtTokenService> _mockJwtService;
    private Mock<IPasswordHasher> _mockPasswordHasher;
    private UserService _userService;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockJwtService = new Mock<IJwtTokenService>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _userService = new UserService(_mockUnitOfWork.Object, _mockJwtService.Object, _mockPasswordHasher.Object);
    }

    [TestMethod]
    public async Task RegisterAsync_ValidUser_ReturnsUserDto()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "password", "Test User");
        _mockUnitOfWork.Setup(x => x.Users.EmailExistsAsync(command.Email)).ReturnsAsync(false);
        
        // Act
        var result = await _userService.RegisterAsync(command);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(command.Email, result.Email);
    }
}
```

### Integration Testing

```csharp
[TestClass]
public class ProductsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProductsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [TestMethod]
    public async Task GetProducts_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/products");
        
        // Assert
        response.EnsureSuccessStatusCode();
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}
```

## Performance Considerations

### Caching Strategy

```csharp
public interface ICacheService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    Task RemoveAsync(string key);
}

// Product caching example
public class CachedProductService : IProductService
{
    private readonly IProductService _productService;
    private readonly ICacheService _cacheService;
    
    public async Task<ProductDto> GetProductByIdAsync(Guid productId)
    {
        var cacheKey = $"product:{productId}";
        var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey);
        
        if (cachedProduct != null)
            return cachedProduct;
            
        var product = await _productService.GetProductByIdAsync(productId);
        await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(15));
        
        return product;
    }
}
```

### Database Optimization

- Use connection pooling for PostgreSQL connections
- Implement read replicas for product catalog queries
- Use database-level pagination for large result sets
- Implement proper indexing strategy as defined in the schema
- Use compiled queries for frequently executed operations

## Logging and Monitoring

### Structured Logging with Serilog

```csharp
public static class LoggerConfiguration
{
    public static void ConfigureSerilog(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.Console()
            .WriteTo.PostgreSQL(
                connectionString: configuration.GetConnectionString("DefaultConnection"),
                tableName: "logs",
                autoCreateSqlTable: true)
            .CreateLogger();
    }
}
```

### Health Checks

```csharp
services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddCheck<PaymentGatewayHealthCheck>("payment-gateway")
    .AddCheck<CacheHealthCheck>("cache");
```

This design provides a solid foundation for implementing the Online Shopping System with proper separation of concerns, testability, and scalability considerations.