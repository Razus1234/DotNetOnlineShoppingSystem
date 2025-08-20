namespace OnlineShoppingSystem.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            // Production CORS policy - restrictive
            options.AddPolicy("ProductionPolicy", policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                    ?? new[] { "https://localhost:3000", "https://localhost:3001" };

                policy.WithOrigins(allowedOrigins)
                      .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                      .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept", "Origin")
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });

            // Development CORS policy - permissive
            options.AddPolicy("DevelopmentPolicy", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            // Mobile app CORS policy
            options.AddPolicy("MobilePolicy", policy =>
            {
                policy.WithOrigins("capacitor://localhost", "ionic://localhost", "http://localhost")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });

            // Default policy based on environment
            if (environment.IsDevelopment())
            {
                options.DefaultPolicyName = "DevelopmentPolicy";
            }
            else
            {
                options.DefaultPolicyName = "ProductionPolicy";
            }
        });

        return services;
    }

    public static IApplicationBuilder UseCorsConfiguration(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            app.UseCors("DevelopmentPolicy");
        }
        else
        {
            app.UseCors("ProductionPolicy");
        }

        return app;
    }
}