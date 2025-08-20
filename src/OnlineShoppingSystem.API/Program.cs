using OnlineShoppingSystem.API.Extensions;
using OnlineShoppingSystem.API.Middleware;
using Serilog;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

try
{
    // Configure enhanced logging first
    builder.ConfigureLogging();

    // Validate configuration early
    builder.Services.ValidateConfiguration(builder.Configuration);

    // Add structured logging services
    builder.Services.AddStructuredLogging();

    // Add database services with migration support
    builder.Services.AddDatabaseServices(builder.Configuration);

    // Register application and infrastructure services
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices();

    // Configure JWT Authentication
    builder.Services.AddJwtAuthentication(builder.Configuration);

    // Configure Authorization policies
    builder.Services.AddAuthorizationPolicies();

    // Add controllers
    builder.Services.AddControllers();

    // Configure Swagger/OpenAPI documentation
    builder.Services.AddSwaggerConfiguration();

    // Configure CORS policies
    builder.Services.AddCorsConfiguration(builder.Configuration, builder.Environment);

    // Add comprehensive health checks
    builder.Services.AddCustomHealthChecks(builder.Configuration);

    Log.Information("Building application...");
    var app = builder.Build();

    // Apply database migrations (skip in test environment)
    if (!app.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase))
    {
        await app.MigrateDatabaseAsync();
    }

    // Configure the HTTP request pipeline
    app.UseSwaggerConfiguration(app.Environment);

    // Add global exception handling middleware
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Add performance logging middleware
    app.UseMiddleware<PerformanceLoggingMiddleware>();

    // Add Serilog request logging with enhanced configuration
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? Serilog.Events.LogEventLevel.Error
            : elapsed > 300
                ? Serilog.Events.LogEventLevel.Warning
                : Serilog.Events.LogEventLevel.Information;
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
            
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
            }
        };
    });

    // Enforce HTTPS
    app.UseHttpsRedirection();

    // Add security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';";
        
        await next();
    });

    // Use CORS configuration
    app.UseCorsConfiguration(app.Environment);

    // Enable static files for Swagger UI custom CSS
    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Map health check endpoints with detailed responses
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Add a simple health check endpoint
    app.MapGet("/", () => "Online Shopping System API is running!")
       .WithName("Root")
       .WithOpenApi();

    Log.Information("Starting Online Shopping System API");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
