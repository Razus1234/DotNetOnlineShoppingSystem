using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineShoppingSystem.API.Middleware;

namespace OnlineShoppingSystem.Tests.Unit.API.Middleware;

[TestClass]
public class PerformanceLoggingMiddlewareTests
{
    [TestMethod]
    public async Task InvokeAsync_ShouldLogRequestPerformance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PerformanceLoggingMiddleware>>();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/products";
        context.Response.StatusCode = 200;

        var requestDelegate = new RequestDelegate(ctx =>
        {
            // Simulate some processing time
            Thread.Sleep(50);
            return Task.CompletedTask;
        });

        var middleware = new PerformanceLoggingMiddleware(requestDelegate, mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        Assert.IsTrue(context.Response.Headers.ContainsKey("X-Response-Time"));
    }

    [TestMethod]
    public async Task InvokeAsync_WhenRequestIsSlow_ShouldLogWarning()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PerformanceLoggingMiddleware>>();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/products";
        context.Response.StatusCode = 200;

        var requestDelegate = new RequestDelegate(ctx =>
        {
            // Simulate slow processing (> 300ms)
            Thread.Sleep(350);
            return Task.CompletedTask;
        });

        var middleware = new PerformanceLoggingMiddleware(requestDelegate, mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Slow request detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldStillLogPerformance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PerformanceLoggingMiddleware>>();
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/orders";
        context.Response.StatusCode = 500;

        var requestDelegate = new RequestDelegate(ctx =>
        {
            throw new InvalidOperationException("Test exception");
        });

        var middleware = new PerformanceLoggingMiddleware(requestDelegate, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        // Verify that performance logging still occurred despite the exception
        mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request completed") || v.ToString()!.Contains("Slow request detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_ShouldAddResponseTimeHeader()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PerformanceLoggingMiddleware>>();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/health";

        var requestDelegate = new RequestDelegate(ctx => Task.CompletedTask);
        var middleware = new PerformanceLoggingMiddleware(requestDelegate, mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.IsTrue(context.Response.Headers.ContainsKey("X-Response-Time"));
        var responseTimeHeader = context.Response.Headers["X-Response-Time"].ToString();
        Assert.IsTrue(responseTimeHeader.Contains("ms"));
    }
}