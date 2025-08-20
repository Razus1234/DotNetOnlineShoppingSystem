using System.Diagnostics;

namespace OnlineShoppingSystem.API.Middleware;

public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;

    public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;

            if (elapsedMilliseconds > 300) // Log slow requests (> 300ms as per requirement 8.1)
            {
                _logger.LogWarning("Slow request detected: {Method} {Path} took {ElapsedMilliseconds}ms with status {StatusCode}",
                    requestMethod, requestPath, elapsedMilliseconds, statusCode);
            }
            else
            {
                _logger.LogInformation("Request completed: {Method} {Path} took {ElapsedMilliseconds}ms with status {StatusCode}",
                    requestMethod, requestPath, elapsedMilliseconds, statusCode);
            }

            // Add performance metrics to response headers for monitoring
            context.Response.Headers["X-Response-Time"] = $"{elapsedMilliseconds}ms";
        }
    }
}