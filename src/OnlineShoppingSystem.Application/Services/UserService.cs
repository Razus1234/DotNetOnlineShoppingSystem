using AutoMapper;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Application.Commands.User;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Exceptions;

namespace OnlineShoppingSystem.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserDto> RegisterAsync(RegisterUserCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        _logger.LogInformation("Starting user registration for email: {Email}", command.Email);

        try
        {
            // Check if email already exists
            var emailExists = await _unitOfWork.Users.EmailExistsAsync(command.Email);
            if (emailExists)
            {
                _logger.LogWarning("Registration failed - email already exists: {Email}", command.Email);
                throw new EmailAlreadyExistsException(command.Email);
            }

            // Hash the password
            var passwordHash = _passwordHasher.HashPassword(command.Password);

            // Create the user
            var user = User.Create(command.Email, passwordHash, command.FullName);

            // Add user to repository
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User registration completed successfully for email: {Email}, UserId: {UserId}", 
                command.Email, user.Id);

            // Map to DTO and return
            return _mapper.Map<UserDto>(user);
        }
        catch (Exception ex) when (!(ex is EmailAlreadyExistsException))
        {
            _logger.LogError(ex, "User registration failed for email: {Email}", command.Email);
            throw;
        }
    }

    public async Task<AuthTokenDto> LoginAsync(LoginCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        _logger.LogInformation("User login attempt for email: {Email}", command.Email);

        try
        {
            // Find user by email
            var user = await _unitOfWork.Users.GetByEmailAsync(command.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found for email: {Email}", command.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed - invalid password for email: {Email}", command.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(user);
            var expiresAt = _jwtTokenService.GetTokenExpiration();

            _logger.LogInformation("User login successful for email: {Email}, UserId: {UserId}", 
                command.Email, user.Id);

            // Map user to DTO
            var userDto = _mapper.Map<UserDto>(user);

            return new AuthTokenDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = userDto
            };
        }
        catch (Exception ex) when (!(ex is UnauthorizedAccessException))
        {
            _logger.LogError(ex, "User login failed for email: {Email}", command.Email);
            throw;
        }
    }

    public async Task<UserDto> GetUserProfileAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        _logger.LogDebug("Retrieving user profile for UserId: {UserId}", userId);

        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User profile not found for UserId: {UserId}", userId);
                throw new UserNotFoundException(userId);
            }

            _logger.LogDebug("User profile retrieved successfully for UserId: {UserId}", userId);
            return _mapper.Map<UserDto>(user);
        }
        catch (Exception ex) when (!(ex is UserNotFoundException))
        {
            _logger.LogError(ex, "Failed to retrieve user profile for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateUserCommand command)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (command == null)
            throw new ArgumentNullException(nameof(command));

        _logger.LogInformation("Updating user profile for UserId: {UserId}", userId);

        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User profile update failed - user not found for UserId: {UserId}", userId);
                throw new UserNotFoundException(userId);
            }

            // Update user profile
            user.UpdateProfile(command.FullName);

            // Save changes
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User profile updated successfully for UserId: {UserId}", userId);
            return _mapper.Map<UserDto>(user);
        }
        catch (Exception ex) when (!(ex is UserNotFoundException))
        {
            _logger.LogError(ex, "Failed to update user profile for UserId: {UserId}", userId);
            throw;
        }
    }
}