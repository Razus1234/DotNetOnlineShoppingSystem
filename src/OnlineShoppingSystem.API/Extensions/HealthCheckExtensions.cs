using Microsoft.Extensions.Diagnostics.HealthChecks;
using OnlineShoppingSystem.API.HealthChecks;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.API.Extensions;

public static class HealthCheckExtensions
{
    public static void AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            // Database health check using Entity Framework
            .AddDbContextCheck<ApplicationDbContext>(
                "database",
                HealthStatus.Unhealthy,
                new[] { "database", "postgresql" })
            
            // PostgreSQL health check using connection string
            .AddNpgSql(
                connectionString: configuration.GetConnectionString("DefaultConnection")!,
                name: "postgresql",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "database", "postgresql" })
            
            // Custom database health check
            .AddCheck<DatabaseHealthCheck>(
                name: "database-custom",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "database", "custom" })
            
            // Payment gateway health check
            .AddCheck<PaymentGatewayHealthCheck>(
                name: "payment-gateway",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "external", "payment" })
            
            // Memory health check
            .AddCheck("memory", () =>
            {
                var allocated = GC.GetTotalMemory(forceFullCollection: false);
                var data = new Dictionary<string, object>()
                {
                    { "AllocatedBytes", allocated },
                    { "Gen0Collections", GC.CollectionCount(0) },
                    { "Gen1Collections", GC.CollectionCount(1) },
                    { "Gen2Collections", GC.CollectionCount(2) }
                };
                
                // Consider unhealthy if allocated memory is over 1GB
                var status = allocated < 1_000_000_000 ? HealthStatus.Healthy : HealthStatus.Degraded;
                
                return HealthCheckResult.Healthy("Memory usage is within acceptable limits", data);
            }, tags: new[] { "memory" });

        // Register custom health check services
        services.AddScoped<DatabaseHealthCheck>();
        services.AddScoped<PaymentGatewayHealthCheck>();
    }
}