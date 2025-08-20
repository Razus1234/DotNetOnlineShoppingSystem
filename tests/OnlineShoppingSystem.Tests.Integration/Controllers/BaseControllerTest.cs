using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Infrastructure.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OnlineShoppingSystem.Tests.Integration.Controllers;

public class BaseControllerTest : IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly ApplicationDbContext DbContext;

    public BaseControllerTest()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database for testing
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });

                    // Reduce logging noise in tests
                    services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
                });
            });

        Client = Factory.CreateClient();
        
        // Get the DbContext from the test server
        var scope = Factory.Services.CreateScope();
        DbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Ensure the database is created
        DbContext.Database.EnsureCreated();
    }

    protected async Task<string> GetJwtTokenAsync(string email = "test@example.com", string password = "TestPassword123!")
    {
        // First register a user if not exists
        var registerRequest = new
        {
            Email = email,
            Password = password,
            FullName = "Test User"
        };

        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json");

        // Try to register (might fail if user already exists, which is fine)
        await Client.PostAsync("/api/auth/register", registerContent);

        // Now login to get the token
        var loginRequest = new
        {
            Email = email,
            Password = password
        };

        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await Client.PostAsync("/api/auth/login", loginContent);
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to login: {errorContent}");
        }

        var loginResult = await loginResponse.Content.ReadAsStringAsync();
        var authToken = JsonSerializer.Deserialize<AuthTokenResponse>(loginResult, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return authToken?.Token ?? throw new InvalidOperationException("No token received");
    }

    protected void SetAuthorizationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    protected StringContent CreateJsonContent(object obj)
    {
        return new StringContent(
            JsonSerializer.Serialize(obj),
            Encoding.UTF8,
            "application/json");
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }

    private class AuthTokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserResponse User { get; set; } = new();
    }

    private class UserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}