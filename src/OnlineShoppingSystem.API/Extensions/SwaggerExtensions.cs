using Microsoft.OpenApi.Models;
using System.Reflection;

namespace OnlineShoppingSystem.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Online Shopping System API",
                Version = "v1",
                Description = "A comprehensive e-commerce backend solution built with .NET 8 and PostgreSQL",
                Contact = new OpenApiContact
                {
                    Name = "Online Shopping System",
                    Email = "support@onlineshoppingsystem.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Add JWT Authentication to Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer' followed by a space and your JWT token. Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments if available
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add operation filters for better documentation
            // options.EnableAnnotations(); // Commented out as it requires additional package
            
            // Custom schema IDs to avoid conflicts
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

            // Add examples for common responses
            options.MapType<Guid>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "uuid",
                Example = new Microsoft.OpenApi.Any.OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6")
            });

            // Group endpoints by tags
            options.TagActionsBy(api =>
            {
                var controllerName = api.ActionDescriptor.RouteValues["controller"];
                return new[] { controllerName ?? "Default" };
            });

            options.DocInclusionPredicate((name, api) => true);
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment() || environment.IsStaging())
        {
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Online Shopping System API v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Online Shopping System API Documentation";
                
                // Enable deep linking
                options.EnableDeepLinking();
                
                // Enable request duration display
                options.DisplayRequestDuration();
                
                // Enable operation ID display
                options.DisplayOperationId();
                
                // Enable validator
                options.EnableValidator();
                
                // Custom CSS for better appearance
                options.InjectStylesheet("/swagger-ui/custom.css");
                
                // Enable try it out by default
                options.EnableTryItOutByDefault();
                
                // Configure OAuth2 if needed in the future
                options.OAuthClientId("swagger-ui");
                options.OAuthAppName("Online Shopping System API");
                options.OAuthUsePkce();
            });
        }

        return app;
    }
}