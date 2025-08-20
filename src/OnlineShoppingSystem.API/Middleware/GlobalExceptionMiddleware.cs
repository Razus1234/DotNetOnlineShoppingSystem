using System.Net;
using System.Text.Json;
using OnlineShoppingSystem.Domain.Exceptions;

namespace OnlineShoppingSystem.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Check if response has already started
        if (context.Response.HasStarted)
        {
            return;
        }

        var response = exception switch
        {
            DomainException domainEx => new ErrorResponse
            {
                Message = domainEx.Message,
                StatusCode = (int)HttpStatusCode.BadRequest,
                Details = domainEx.GetType().Name
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                Message = "Unauthorized access",
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Details = "Authentication failed"
            },
            ArgumentException argEx => new ErrorResponse
            {
                Message = argEx.Message,
                StatusCode = (int)HttpStatusCode.BadRequest,
                Details = "Invalid argument"
            },
            KeyNotFoundException => new ErrorResponse
            {
                Message = "Resource not found",
                StatusCode = (int)HttpStatusCode.NotFound,
                Details = "The requested resource was not found"
            },
            _ => new ErrorResponse
            {
                Message = "An internal server error occurred",
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Details = "Please try again later"
            }
        };

        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = "application/json";

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}