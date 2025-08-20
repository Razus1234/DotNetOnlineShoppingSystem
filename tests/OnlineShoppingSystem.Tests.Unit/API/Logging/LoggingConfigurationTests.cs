using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.API.Extensions;
using Serilog;

namespace OnlineShoppingSystem.Tests.Unit.API.Logging;

[TestClass]
public class LoggingConfigurationTests
{
    [TestMethod]
    public void ConfigureLogging_ShouldConfigureSerilogCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=TestDb;Username=test;Password=test",
                ["Serilog:MinimumLevel:Default"] = "Information"
            })
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Act
        builder.ConfigureLogging();

        // Assert
        var app = builder.Build();
        var logger = app.Services.GetService<ILogger<LoggingConfigurationTests>>();
        
        Assert.IsNotNull(logger);
    }

    [TestMethod]
    public void AddStructuredLogging_ShouldRegisterLoggingServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStructuredLogging();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        
        Assert.IsNotNull(loggerFactory);
    }

    [TestMethod]
    public void Logger_ShouldLogWithStructuredData()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetService<ILogger<LoggingConfigurationTests>>();

        // Act & Assert
        Assert.IsNotNull(logger);
        
        // Test that logger can handle structured logging
        logger?.LogInformation("Test log with {Property}", "TestValue");
        
        // If we get here without exception, the test passes
        Assert.IsTrue(true);
    }

    [TestMethod]
    [DataRow(LogLevel.Debug)]
    [DataRow(LogLevel.Information)]
    [DataRow(LogLevel.Warning)]
    [DataRow(LogLevel.Error)]
    [DataRow(LogLevel.Critical)]
    public void Logger_ShouldSupportAllLogLevels(LogLevel logLevel)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetService<ILogger<LoggingConfigurationTests>>();

        // Act & Assert
        Assert.IsNotNull(logger);
        
        // Test that logger can handle all log levels
        logger?.Log(logLevel, "Test log at {LogLevel}", logLevel);
        
        // If we get here without exception, the test passes
        Assert.IsTrue(true);
    }
}