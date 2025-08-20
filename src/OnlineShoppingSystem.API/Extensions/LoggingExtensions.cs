using Serilog;
using Serilog.Events;

namespace OnlineShoppingSystem.API.Extensions;

public static class LoggingExtensions
{
    public static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        // Configure Serilog with enhanced settings
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()

                .Enrich.WithEnvironmentName()
                .Enrich.WithProcessId()
                .Enrich.WithProperty("Application", "OnlineShoppingSystem")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.PostgreSQL(
                    connectionString: context.Configuration.GetConnectionString("DefaultConnection")!,
                    tableName: "logs",
                    restrictedToMinimumLevel: LogEventLevel.Information);

            // Set minimum log levels based on environment
            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.MinimumLevel.Debug();
            }
            else
            {
                configuration.MinimumLevel.Information();
            }

            // Override specific namespaces
            configuration
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);
        });
    }

    public static void AddStructuredLogging(this IServiceCollection services)
    {
        // Add any additional logging services here
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });
    }
}