using OnlineShoppingSystem.Application.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSystem.API.Extensions;

public static class ConfigurationExtensions
{
    public static void ValidateConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Validate JWT Settings
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
        ValidateJwtSettings(jwtSettings);

        // Validate Connection Strings
        ValidateConnectionStrings(configuration);

        // Validate Stripe Settings
        ValidateStripeSettings(configuration);

        // Validate CORS Settings
        ValidateCorsSettings(configuration);

        // Register validated configurations
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
    }

    private static void ValidateJwtSettings(JwtSettings? jwtSettings)
    {
        if (jwtSettings == null)
            throw new InvalidOperationException("JWT settings are not configured");

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(jwtSettings);

        if (!Validator.TryValidateObject(jwtSettings, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"JWT settings validation failed: {errors}");
        }

        // Additional JWT-specific validations
        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
            throw new InvalidOperationException("JWT SecretKey is required");

        if (jwtSettings.SecretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");

        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
            throw new InvalidOperationException("JWT Issuer is required");

        if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
            throw new InvalidOperationException("JWT Audience is required");

        if (jwtSettings.ExpirationHours <= 0)
            throw new InvalidOperationException("JWT ExpirationHours must be greater than 0");
    }

    private static void ValidateConnectionStrings(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("DefaultConnection connection string is required");

        // Basic validation for PostgreSQL connection string
        if (!connectionString.Contains("Host=") || !connectionString.Contains("Database="))
            throw new InvalidOperationException("Invalid PostgreSQL connection string format");
    }

    private static void ValidateStripeSettings(IConfiguration configuration)
    {
        var stripeSection = configuration.GetSection("Stripe");
        var secretKey = stripeSection["SecretKey"];
        var publishableKey = stripeSection["PublishableKey"];

        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Stripe SecretKey is required");

        if (string.IsNullOrWhiteSpace(publishableKey))
            throw new InvalidOperationException("Stripe PublishableKey is required");

        // Validate Stripe key formats
        if (!secretKey.StartsWith("sk_"))
            throw new InvalidOperationException("Invalid Stripe SecretKey format");

        if (!publishableKey.StartsWith("pk_"))
            throw new InvalidOperationException("Invalid Stripe PublishableKey format");
    }

    private static void ValidateCorsSettings(IConfiguration configuration)
    {
        // CORS settings are optional but if provided, validate them
        var corsSection = configuration.GetSection("Cors");
        if (corsSection.Exists())
        {
            var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>();
            if (allowedOrigins != null)
            {
                foreach (var origin in allowedOrigins)
                {
                    if (!Uri.TryCreate(origin, UriKind.Absolute, out _))
                        throw new InvalidOperationException($"Invalid CORS origin format: {origin}");
                }
            }
        }
    }
}

public class StripeSettings
{
    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string PublishableKey { get; set; } = string.Empty;
}