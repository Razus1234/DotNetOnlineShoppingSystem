using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;
using OnlineShoppingSystem.Domain.Enums;
using OnlineShoppingSystem.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace OnlineShoppingSystem.Tests.Integration.Data;

[TestClass]
public class PostgreSqlDbContextTests
{
    private PostgreSqlContainer _postgresContainer = null!;
    private ServiceProvider _serviceProvider = null!;
    private ApplicationDbContext _context = null!;

    [TestInitialize]
    public async Task Setup()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();

        await _postgresContainer.StartAsync();

        var services = new ServiceCollection();
        
        // Use PostgreSQL test container
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(_postgresContainer.GetConnectionString()));

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Apply migrations
        await _context.Database.MigrateAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
        
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task DatabaseIndexesAreCreatedCorrectly()
    {
        // Act - Query the PostgreSQL system tables to verify indexes exist
        var indexQuery = @"
            SELECT 
                schemaname,
                tablename,
                indexname,
                indexdef
            FROM pg_indexes 
            WHERE schemaname = 'public' 
            AND indexname LIKE 'IX_%'
            ORDER BY tablename, indexname";

        var indexes = await _context.Database.SqlQueryRaw<IndexInfo>(indexQuery).ToListAsync();

        // Assert - Verify key indexes exist
        var indexNames = indexes.Select(i => i.indexname).ToList();
        
        // User indexes
        Assert.IsTrue(indexNames.Contains("IX_users_email"), "Email index should exist");
        
        // Product indexes
        Assert.IsTrue(indexNames.Contains("IX_products_category"), "Category index should exist");
        Assert.IsTrue(indexNames.Contains("IX_products_name"), "Name index should exist");
        
        // Order indexes
        Assert.IsTrue(indexNames.Contains("IX_orders_user_id"), "Order user_id index should exist");
        Assert.IsTrue(indexNames.Contains("IX_orders_status"), "Order status index should exist");
        
        // Cart indexes
        Assert.IsTrue(indexNames.Contains("IX_carts_user_id"), "Cart user_id index should exist");
        
        // Payment indexes
        Assert.IsTrue(indexNames.Contains("IX_payments_order_id"), "Payment order_id index should exist");
        Assert.IsTrue(indexNames.Contains("IX_payments_status"), "Payment status index should exist");
        
        // CreatedAt indexes for performance
        Assert.IsTrue(indexNames.Any(n => n.Contains("CreatedAt")), "CreatedAt indexes should exist");
    }

    [TestMethod]
    public async Task UniqueConstraintsAreEnforced()
    {
        // Arrange
        var user1 = new User("unique@test.com", "hashedpassword123", "User One");
        var user2 = new User("unique@test.com", "hashedpassword456", "User Two");
        
        _context.Users.Add(user1);
        await _context.SaveChangesAsync();
        
        // Act & Assert
        _context.Users.Add(user2);
        
        var exception = await Assert.ThrowsExceptionAsync<DbUpdateException>(async () =>
        {
            await _context.SaveChangesAsync();
        });
        
        Assert.IsTrue(exception.InnerException?.Message.Contains("duplicate key") == true);
    }

    [TestMethod]
    public async Task ForeignKeyConstraintsAreEnforced()
    {
        // Arrange - Try to create a cart with non-existent user
        var nonExistentUserId = Guid.NewGuid();
        var cart = new Cart(nonExistentUserId);
        
        // Act & Assert
        _context.Carts.Add(cart);
        
        var exception = await Assert.ThrowsExceptionAsync<DbUpdateException>(async () =>
        {
            await _context.SaveChangesAsync();
        });
        
        Assert.IsTrue(exception.InnerException?.Message.Contains("foreign key") == true);
    }

    [TestMethod]
    public async Task CascadeDeleteWorksCorrectly()
    {
        // Arrange
        var user = new User("cascade@test.com", "hashedpassword123", "Cascade User");
        user.CreateCart();
        
        var price = new Money(25.00m, "USD");
        var product = new Product("Cascade Product", "Test product for cascade", price, 5, "Test");
        
        _context.Users.Add(user);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        user.Cart!.AddItem(product, 2);
        await _context.SaveChangesAsync();
        
        var cartId = user.Cart.Id;
        var cartItemCount = await _context.CartItems.CountAsync(ci => ci.CartId == cartId);
        Assert.AreEqual(1, cartItemCount);
        
        // Act - Delete user (should cascade to cart and cart items)
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        
        // Assert
        var cartExists = await _context.Carts.AnyAsync(c => c.Id == cartId);
        var cartItemsExist = await _context.CartItems.AnyAsync(ci => ci.CartId == cartId);
        
        Assert.IsFalse(cartExists, "Cart should be deleted when user is deleted");
        Assert.IsFalse(cartItemsExist, "Cart items should be deleted when cart is deleted");
    }

    [TestMethod]
    public async Task MoneyValueObjectsAreStoredCorrectly()
    {
        // Arrange
        var price = new Money(123.45m, "EUR");
        var product = new Product("Euro Product", "Product with EUR price", price, 3, "International");
        
        // Act
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        // Query directly from database to verify storage
        var storedProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Name == "Euro Product");
        
        // Assert
        Assert.IsNotNull(storedProduct);
        Assert.AreEqual(123.45m, storedProduct.Price.Amount);
        Assert.AreEqual("EUR", storedProduct.Price.Currency);
    }

    [TestMethod]
    public async Task AddressValueObjectsAreStoredCorrectly()
    {
        // Arrange
        var user = new User("address@test.com", "hashedpassword123", "Address User");
        var address1 = new Address("123 Main St", "Anytown", "12345", "USA");
        var address2 = new Address("456 Oak Ave", "Somewhere", "67890", "Canada");
        
        user.AddAddress(address1);
        user.AddAddress(address2);
        
        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        // Query with addresses
        var storedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == "address@test.com");
        
        // Assert
        Assert.IsNotNull(storedUser);
        Assert.AreEqual(2, storedUser.Addresses.Count);
        
        var usaAddress = storedUser.Addresses.First(a => a.Country == "USA");
        Assert.AreEqual("123 Main St", usaAddress.Street);
        Assert.AreEqual("Anytown", usaAddress.City);
        Assert.AreEqual("12345", usaAddress.PostalCode);
        
        var canadaAddress = storedUser.Addresses.First(a => a.Country == "Canada");
        Assert.AreEqual("456 Oak Ave", canadaAddress.Street);
        Assert.AreEqual("Somewhere", canadaAddress.City);
        Assert.AreEqual("67890", canadaAddress.PostalCode);
    }

    [TestMethod]
    public async Task EnumConversionsWorkCorrectly()
    {
        // Arrange
        var user = new User("enum@test.com", "hashedpassword123", "Enum User");
        var address = new Address("789 Enum St", "Enum City", "11111", "Enum Country");
        
        var price = new Money(75.00m, "USD");
        var product = new Product("Enum Product", "Product for enum testing", price, 2, "Test");
        
        _context.Users.Add(user);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        var orderItems = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), product.Id, product.Name, product.Price, 1)
        };
        
        var order = new Order(user.Id, address, orderItems);
        var payment = new Payment(order.Id, order.Total, "Credit Card");
        
        // Test different enum states
        order.UpdateStatus(OrderStatus.Confirmed);
        order.UpdateStatus(OrderStatus.Processing);
        
        payment.MarkAsProcessing("txn_enum_test");
        payment.MarkAsCompleted();
        
        _context.Orders.Add(order);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        
        // Act - Query and verify enum storage
        var storedOrder = await _context.Orders.FirstOrDefaultAsync(o => o.UserId == user.Id);
        var storedPayment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
        
        // Assert
        Assert.IsNotNull(storedOrder);
        Assert.AreEqual(OrderStatus.Processing, storedOrder.Status);
        
        Assert.IsNotNull(storedPayment);
        Assert.AreEqual(PaymentStatus.Completed, storedPayment.Status);
    }

    [TestMethod]
    public async Task ComplexQueryPerformanceWithIndexes()
    {
        // Arrange - Create test data
        var users = new List<User>();
        var products = new List<Product>();
        
        for (int i = 0; i < 100; i++)
        {
            users.Add(new User($"user{i}@test.com", "hashedpassword123", $"User {i}"));
            
            var price = new Money(10.00m + i, "USD");
            products.Add(new Product($"Product {i}", $"Description for product {i}", price, i % 10, i % 5 == 0 ? "Electronics" : "Books"));
        }
        
        _context.Users.AddRange(users);
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
        
        // Act - Perform complex queries that should use indexes
        var startTime = DateTime.UtcNow;
        
        // Query by email (should use email index)
        var userByEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == "user50@test.com");
        
        // Query by category (should use category index)
        var electronicsProducts = await _context.Products
            .Where(p => p.Category == "Electronics")
            .ToListAsync();
        
        // Query by date range (should use CreatedAt index)
        var recentProducts = await _context.Products
            .Where(p => p.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
            .ToListAsync();
        
        var endTime = DateTime.UtcNow;
        var queryTime = endTime - startTime;
        
        // Assert
        Assert.IsNotNull(userByEmail);
        Assert.AreEqual("user50@test.com", userByEmail.Email);
        
        Assert.IsTrue(electronicsProducts.Count > 0);
        Assert.IsTrue(electronicsProducts.All(p => p.Category == "Electronics"));
        
        Assert.AreEqual(100, recentProducts.Count); // All products should be recent
        
        // Performance assertion - queries should complete quickly with proper indexes
        Assert.IsTrue(queryTime.TotalSeconds < 5, $"Queries took too long: {queryTime.TotalSeconds} seconds");
    }

    // Helper class for index information query
    public class IndexInfo
    {
        public string schemaname { get; set; } = string.Empty;
        public string tablename { get; set; } = string.Empty;
        public string indexname { get; set; } = string.Empty;
        public string indexdef { get; set; } = string.Empty;
    }
}