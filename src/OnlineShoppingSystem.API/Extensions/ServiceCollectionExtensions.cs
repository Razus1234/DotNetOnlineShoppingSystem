using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Common.Models;
using OnlineShoppingSystem.Application.Interfaces;
using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Infrastructure.Data;
using OnlineShoppingSystem.Infrastructure.Data.Repositories;
using OnlineShoppingSystem.Infrastructure.Services;
using System.Text;

namespace OnlineShoppingSystem.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register core application services
        services.AddScoped<UserService>();
        services.AddScoped<ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();

        // Register cached services as decorators
        services.AddScoped<IUserService>(provider =>
        {
            var userService = provider.GetRequiredService<UserService>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            var logger = provider.GetRequiredService<ILogger<CachedUserService>>();
            return new CachedUserService(userService, cacheService, logger);
        });

        services.AddScoped<IProductService>(provider =>
        {
            var productService = provider.GetRequiredService<ProductService>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            var logger = provider.GetRequiredService<ILogger<CachedProductService>>();
            return new CachedProductService(productService, cacheService, logger);
        });

        // Add AutoMapper
        services.AddAutoMapper(typeof(OnlineShoppingSystem.Application.Mappings.CartMappingProfile));

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register infrastructure services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Register payment gateway
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();

        // Register caching services
        services.AddScoped<ICacheService, InMemoryCacheService>();

        // Add memory cache for caching services
        services.AddMemoryCache();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
        if (jwtSettings == null)
        {
            throw new InvalidOperationException("JWT settings are not configured properly");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true
            };

            // Add event handlers for better logging
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("JWT token validated for user: {UserId}", 
                        context.Principal?.FindFirst("sub")?.Value ?? "Unknown");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT authentication challenge: {Error}", context.Error);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Admin-only policy
            options.AddPolicy("AdminOnly", policy => 
                policy.RequireRole("Admin")
                      .RequireAuthenticatedUser());
                      
            // Customer-only policy
            options.AddPolicy("CustomerOnly", policy => 
                policy.RequireRole("Customer")
                      .RequireAuthenticatedUser());
                      
            // Admin or Customer policy
            options.AddPolicy("AdminOrCustomer", policy =>
                policy.RequireRole("Admin", "Customer")
                      .RequireAuthenticatedUser());
                      
            // Require authenticated user policy
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());

            // Resource-based policies
            options.AddPolicy("CanManageProducts", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("CanViewOrders", policy =>
                policy.RequireRole("Admin", "Customer"));

            options.AddPolicy("CanManageOrders", policy =>
                policy.RequireRole("Admin"));
        });

        return services;
    }
}