using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.API.Extensions;

public static class DatabaseExtensions
{
    public static async Task<IHost> MigrateDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Starting database migration...");
            
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            // Check if this is a relational database provider
            if (context.Database.IsRelational())
            {
                // Apply any pending migrations for relational databases
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                        pendingMigrations.Count(), 
                        string.Join(", ", pendingMigrations));
                    
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Database migration completed successfully");
                }
                else
                {
                    logger.LogInformation("No pending migrations found");
                }
            }
            else
            {
                // For non-relational databases (like in-memory), just ensure created
                logger.LogInformation("Non-relational database detected, ensuring database is created");
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Database creation completed successfully");
            }

            // Verify database connection
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                throw new InvalidOperationException("Cannot connect to database after migration");
            }

            logger.LogInformation("Database connection verified successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }

        return host;
    }

    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("OnlineShoppingSystem.Infrastructure");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            // Enable sensitive data logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Register the DbContext interface
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}