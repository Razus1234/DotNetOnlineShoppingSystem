using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSystem.API.Attributes;
using OnlineShoppingSystem.Application.Commands.User;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using System.Security.Claims;

namespace OnlineShoppingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="command">User registration details</param>
    /// <returns>Created user information</returns>
    [HttpPost("register")]
    [SanitizeInput]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterUserCommand command)
    {
        _logger.LogInformation("User registration attempt for email: {Email}", command.Email);

        var user = await _userService.RegisterAsync(command);

        _logger.LogInformation("User registered successfully with ID: {UserId}", user.Id);

        return CreatedAtAction(nameof(GetProfile), new { }, user);
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    /// <param name="command">Login credentials</param>
    /// <returns>Authentication token and user information</returns>
    [HttpPost("login")]
    [SanitizeInput]
    [ProducesResponseType(typeof(AuthTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenDto>> Login([FromBody] LoginCommand command)
    {
        _logger.LogInformation("Login attempt for email: {Email}", command.Email);

        var authToken = await _userService.LoginAsync(command);

        _logger.LogInformation("User logged in successfully: {UserId}", authToken.User.Id);

        return Ok(authToken);
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Getting profile for user: {UserId}", userId);

        var user = await _userService.GetUserProfileAsync(userId);

        return Ok(user);
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    /// <param name="command">Profile update details</param>
    /// <returns>Updated user information</returns>
    [HttpPut("profile")]
    [Authorize]
    [SanitizeInput]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateUserCommand command)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Updating profile for user: {UserId}", userId);

        var user = await _userService.UpdateProfileAsync(userId, command);

        _logger.LogInformation("Profile updated successfully for user: {UserId}", userId);

        return Ok(user);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }
}