using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text.Json;

namespace OnlineShoppingSystem.Tests.Integration.Controllers;

[TestClass]
public class AuthControllerTests : BaseControllerTest
{
    [TestMethod]
    public async Task Register_ValidUser_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            Email = "newuser@example.com",
            Password = "NewPassword123!",
            FullName = "New User"
        };

        // Act
        var response = await Client.PostAsync("/api/auth/register", CreateJsonContent(request));

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.IsNotNull(user);
        Assert.AreEqual(request.Email, user.Email);
        Assert.AreEqual(request.FullName, user.FullName);
    }

    [TestMethod]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "duplicate@example.com",
            Password = "Password123!",
            FullName = "Duplicate User"
        };

        // Register user first time
        await Client.PostAsync("/api/auth/register", CreateJsonContent(request));

        // Act - Try to register same email again
        var response = await Client.PostAsync("/api/auth/register", CreateJsonContent(request));

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "invalid-email",
            Password = "Password123!",
            FullName = "Test User"
        };

        // Act
        var response = await Client.PostAsync("/api/auth/register", CreateJsonContent(request));

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var email = "login@example.com";
        var password = "LoginPassword123!";
        
        // Register user first
        var registerRequest = new
        {
            Email = email,
            Password = password,
            FullName = "Login User"
        };
        await Client.PostAsync("/api/auth/register", CreateJsonContent(registerRequest));

        var loginRequest = new
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await Client.PostAsync("/api/auth/login", CreateJsonContent(loginRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var authToken = JsonSerializer.Deserialize<AuthTokenResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.IsNotNull(authToken);
        Assert.IsFalse(string.IsNullOrEmpty(authToken.Token));
        Assert.IsTrue(authToken.ExpiresAt > DateTime.UtcNow);
        Assert.IsNotNull(authToken.User);
        Assert.AreEqual(email, authToken.User.Email);
    }

    [TestMethod]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await Client.PostAsync("/api/auth/login", CreateJsonContent(request));

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetProfile_WithValidToken_ReturnsUserProfile()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await Client.GetAsync("/api/auth/profile");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var user = await DeserializeResponseAsync<UserResponse>(response);
        Assert.IsNotNull(user);
        Assert.AreEqual("test@example.com", user.Email);
        Assert.AreEqual("Test User", user.FullName);
    }

    [TestMethod]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/profile");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProfile_WithValidToken_ReturnsUpdatedProfile()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var updateRequest = new
        {
            FullName = "Updated Test User"
        };

        // Act
        var response = await Client.PutAsync("/api/auth/profile", CreateJsonContent(updateRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var user = await DeserializeResponseAsync<UserResponse>(response);
        Assert.IsNotNull(user);
        Assert.AreEqual("Updated Test User", user.FullName);
    }

    [TestMethod]
    public async Task UpdateProfile_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var updateRequest = new
        {
            FullName = "Updated Test User"
        };

        // Act
        var response = await Client.PutAsync("/api/auth/profile", CreateJsonContent(updateRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private class UserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    private class AuthTokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserResponse User { get; set; } = new();
    }
}