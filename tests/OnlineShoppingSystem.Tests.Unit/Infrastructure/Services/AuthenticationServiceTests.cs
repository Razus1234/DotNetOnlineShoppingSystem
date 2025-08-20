using AutoMapper;
using Moq;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Commands.User;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Exceptions;
using OnlineShoppingSystem.Infrastructure.Services;

namespace OnlineShoppingSystem.Tests.Unit.Infrastructure.Services;

[TestClass]
public class AuthenticationServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IJwtTokenService> _mockJwtTokenService;
    private Mock<IPasswordHasher> _mockPasswordHasher;
    private Mock<IMapper> _mockMapper;
    private Mock<IUserRepository> _mockUserRepository;
    private AuthenticationService _authenticationService;
    private const string ValidPasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj/VcSAg/9qK";

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockMapper = new Mock<IMapper>();
        _mockUserRepository = new Mock<IUserRepository>();

        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);

        _authenticationService = new AuthenticationService(
            _mockUnitOfWork.Object,
            _mockJwtTokenService.Object,
            _mockPasswordHasher.Object,
            _mockMapper.Object);
    }

    [TestMethod]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthTokenDto()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var user = User.Create("test@example.com", ValidPasswordHash, "Test User");
        var token = "jwt-token";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName
        };

        _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);
        _mockJwtTokenService.Setup(x => x.GenerateToken(user))
            .Returns(token);
        _mockJwtTokenService.Setup(x => x.GetTokenExpiration())
            .Returns(expiresAt);
        _mockMapper.Setup(x => x.Map<UserDto>(user))
            .Returns(userDto);

        // Act
        var result = await _authenticationService.LoginAsync(command);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(token, result.Token);
        Assert.AreEqual(expiresAt, result.ExpiresAt);
        Assert.AreEqual(userDto, result.User);
    }

    [TestMethod]
    [ExpectedException(typeof(UserNotFoundException))]
    public async Task LoginAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);

        // Act
        await _authenticationService.LoginAsync(command);
    }

    [TestMethod]
    [ExpectedException(typeof(UnauthorizedAccessException))]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        var user = User.Create("test@example.com", ValidPasswordHash, "Test User");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(false);

        // Act
        await _authenticationService.LoginAsync(command);
    }

    [TestMethod]
    public async Task ValidateUserCredentialsAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password123";
        var user = User.Create(email, ValidPasswordHash, "Test User");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(password, user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _authenticationService.ValidateUserCredentialsAsync(email, password);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ValidateUserCredentialsAsync_UserNotFound_ReturnsFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "password123";

        _mockUserRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authenticationService.ValidateUserCredentialsAsync(email, password);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ValidateUserCredentialsAsync_InvalidPassword_ReturnsFalse()
    {
        // Arrange
        var email = "test@example.com";
        var password = "wrongpassword";
        var user = User.Create(email, ValidPasswordHash, "Test User");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(password, user.PasswordHash))
            .Returns(false);

        // Act
        var result = await _authenticationService.ValidateUserCredentialsAsync(email, password);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task LoginAsync_CallsAllDependenciesCorrectly()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var user = User.Create("test@example.com", ValidPasswordHash, "Test User");
        var token = "jwt-token";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var userDto = new UserDto();

        _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);
        _mockJwtTokenService.Setup(x => x.GenerateToken(user))
            .Returns(token);
        _mockJwtTokenService.Setup(x => x.GetTokenExpiration())
            .Returns(expiresAt);
        _mockMapper.Setup(x => x.Map<UserDto>(user))
            .Returns(userDto);

        // Act
        await _authenticationService.LoginAsync(command);

        // Assert
        _mockUserRepository.Verify(x => x.GetByEmailAsync(command.Email), Times.Once);
        _mockPasswordHasher.Verify(x => x.VerifyPassword(command.Password, user.PasswordHash), Times.Once);
        _mockJwtTokenService.Verify(x => x.GenerateToken(user), Times.Once);
        _mockJwtTokenService.Verify(x => x.GetTokenExpiration(), Times.Once);
        _mockMapper.Verify(x => x.Map<UserDto>(user), Times.Once);
    }

    [TestMethod]
    public async Task ValidateUserCredentialsAsync_CallsAllDependenciesCorrectly()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password123";
        var user = User.Create(email, ValidPasswordHash, "Test User");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(password, user.PasswordHash))
            .Returns(true);

        // Act
        await _authenticationService.ValidateUserCredentialsAsync(email, password);

        // Assert
        _mockUserRepository.Verify(x => x.GetByEmailAsync(email), Times.Once);
        _mockPasswordHasher.Verify(x => x.VerifyPassword(password, user.PasswordHash), Times.Once);
    }
}