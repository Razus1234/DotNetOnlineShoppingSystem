using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Tests.Integration.Data;

[TestClass]
public class DatabaseConfigurationTests
{
    [TestMethod]
    public void DbContextCanBeConfiguredWithPostgreSQL()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Host=localhost;Database=TestDb;Username=testuser;Password=testpass";
        
        // Act
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var context = serviceProvider.GetService<ApplicationDbContext>();
        Assert.IsNotNull(context);
        Assert.IsTrue(context.Database.IsNpgsql());
        
        serviceProvider.Dispose();
    }

    [TestMethod]
    public void DbContextHasCorrectDbSets()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDbSets"));
        
        var serviceProvider = services.BuildServiceProvider();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Act & Assert
        Assert.IsNotNull(context.Users);
        Assert.IsNotNull(context.Products);
        Assert.IsNotNull(context.Carts);
        Assert.IsNotNull(context.CartItems);
        Assert.IsNotNull(context.Orders);
        Assert.IsNotNull(context.OrderItems);
        Assert.IsNotNull(context.Payments);
        
        serviceProvider.Dispose();
    }

    [TestMethod]
    public void EntityConfigurationsAreApplied()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestConfigurations"));
        
        var serviceProvider = services.BuildServiceProvider();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Act
        var model = context.Model;
        
        // Assert - Check that entity configurations are applied
        var userEntityType = model.FindEntityType(typeof(Domain.Entities.User));
        Assert.IsNotNull(userEntityType);
        Assert.AreEqual("users", userEntityType.GetTableName());
        
        var productEntityType = model.FindEntityType(typeof(Domain.Entities.Product));
        Assert.IsNotNull(productEntityType);
        Assert.AreEqual("products", productEntityType.GetTableName());
        
        var orderEntityType = model.FindEntityType(typeof(Domain.Entities.Order));
        Assert.IsNotNull(orderEntityType);
        Assert.AreEqual("orders", orderEntityType.GetTableName());
        
        var cartEntityType = model.FindEntityType(typeof(Domain.Entities.Cart));
        Assert.IsNotNull(cartEntityType);
        Assert.AreEqual("carts", cartEntityType.GetTableName());
        
        var paymentEntityType = model.FindEntityType(typeof(Domain.Entities.Payment));
        Assert.IsNotNull(paymentEntityType);
        Assert.AreEqual("payments", paymentEntityType.GetTableName());
        
        serviceProvider.Dispose();
    }

    [TestMethod]
    public void ValueObjectsAreConfiguredAsOwned()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestValueObjects"));
        
        var serviceProvider = services.BuildServiceProvider();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Act
        var model = context.Model;
        
        // Assert - Check that entity types exist and are properly configured
        var productEntityType = model.FindEntityType(typeof(Domain.Entities.Product));
        Assert.IsNotNull(productEntityType);
        
        var userEntityType = model.FindEntityType(typeof(Domain.Entities.User));
        Assert.IsNotNull(userEntityType);
        
        // Check that Address value objects are configured as owned entities
        var addressEntityTypes = model.GetEntityTypes()
            .Where(et => et.ClrType == typeof(Domain.ValueObjects.Address))
            .ToList();
        
        Assert.IsTrue(addressEntityTypes.Any(), "Address value objects should be configured as owned entities");
        
        // Verify that the model can be created without errors (which means configurations are valid)
        Assert.IsNotNull(model);
        Assert.IsTrue(model.GetEntityTypes().Any(), "Model should contain entity types");
        
        serviceProvider.Dispose();
    }
}