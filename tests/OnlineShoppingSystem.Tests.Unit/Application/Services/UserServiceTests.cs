using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OnlineShoppingSystem.Application.Commands.User;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Exceptions;

namespace OnlineShoppingSystem.Tests.Unit.Application.Services;

[TestClass]
public class UserServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IPasswordHasher> _mockPasswordHasher = null!;
    private Mock<IJwtTokenService> _mockJwtTokenService = null!;
    private Mock<IMapper> _mockMapper = null!;
    private UserService _userService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);

        _userService = new UserService(
            _mockUnitOfWork.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenService.Object,
            _mockMapper.Object,
            Mock.Of<ILogger<UserService>>());
    }

    [TestMethod]
    public async Task RegisterAsync_ValidCommand_ReturnsUserDto()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "password123",
            FullName = "Test User"
        };

        var hashedPassword = "$2a$11$K2CtDP7zSGOKgjXjxD8eAOqP9QzJvd1/JFCdEYNZZjgUYzZzZzZzZ";
        var user = User.Create(command.Email, hashedPassword, command.FullName);
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = command.Email,
            FullName = command.FullName
        };

        _mockUserRepository.Setup(x => x.EmailExistsAsync(command.Email))
            .ReturnsAsync(false);
        _mockPasswordHasher.Setup(x => x.HashPassword(command.Password))
            .Returns(hashedPassword);
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);

        // Act
        var result = await _userService.RegisterAsync(command);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(command.Email, result.Email);
        Assert.AreEqual(command.FullName, result.FullName);
    }

    [TestMethod]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsEmailAlreadyExistsException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "existing@example.com",
            Password = "password123",
            FullName = "Test User"
        };

        _mockUserRepository.Setup(x => x.EmailExistsAsync(command.Email))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<EmailAlreadyExistsException>(
            () => _userService.RegisterAsync(command));

        Assert.AreEqual($"User with email '{command.Email}' already exists", exception.Message);
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

        var hashedPassword = "$2a$11$K2CtDP7zSGOKgjXjxD8eAOqP9QzJvd1/JFCdEYNZZjgUYzZzZzZzZ";
        var user = User.Create(command.Email, hashedPassword, "Test User");
        var token = "jwt-token";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = command.Email,
            FullName = "Test User"
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
        var result = await _userService.LoginAsync(command);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(token, result.Token);
        Assert.AreEqual(expiresAt, result.ExpiresAt);
        Assert.AreEqual(userDto, result.User);
    }

    [TestMethod]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
            () => _userService.LoginAsync(command));

        Assert.AreEqual("Invalid email or password", exception.Message);
    }

    [TestMethod]
    public async Task GetUserProfileAsync_ValidUserId_ReturnsUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "$2a$11$K2CtDP7zSGOKgjXjxD8eAOqP9QzJvd1/JFCdEYNZZjgUYzZzZzZzZ", "Test User");
        var userDto = new UserDto
        {
            Id = userId,
            Email = "test@example.com",
            FullName = "Test User"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockMapper.Setup(x => x.Map<UserDto>(user))
            .Returns(userDto);

        // Act
        var result = await _userService.GetUserProfileAsync(userId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(userDto, result);
    }

    [TestMethod]
    public async Task GetUserProfileAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<UserNotFoundException>(
            () => _userService.GetUserProfileAsync(userId));

        Assert.AreEqual($"User with ID {userId} was not found", exception.Message);
    }

    [TestMethod]
    public async Task UpdateProfileAsync_ValidCommand_ReturnsUpdatedUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand
        {
            FullName = "Updated Name"
        };

        var user = User.Create("test@example.com", "$2a$11$K2CtDP7zSGOKgjXjxD8eAOqP9QzJvd1/JFCdEYNZZjgUYzZzZzZzZ", "Original Name");
        var updatedUserDto = new UserDto
        {
            Id = userId,
            Email = "test@example.com",
            FullName = "Updated Name"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateAsync(user))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockMapper.Setup(x => x.Map<UserDto>(user))
            .Returns(updatedUserDto);

        // Act
        var result = await _userService.UpdateProfileAsync(userId, command);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(updatedUserDto, result);
    }

    [TestMethod]
    public async Task UpdateProfileAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand
        {
            FullName = "Updated Name"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<UserNotFoundException>(
            () => _userService.UpdateProfileAsync(userId, command));

        Assert.AreEqual($"User with ID {userId} was not found", exception.Message);
    }

    [TestMethod]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new UserService(null!, _mockPasswordHasher.Object, _mockJwtTokenService.Object, _mockMapper.Object, Mock.Of<ILogger<UserService>>()));
    }

    [TestMethod]
    public void Constructor_NullPasswordHasher_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new UserService(_mockUnitOfWork.Object, null!, _mockJwtTokenService.Object, _mockMapper.Object, Mock.Of<ILogger<UserService>>()));
    }

    [TestMethod]
    public void Constructor_NullJwtTokenService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new UserService(_mockUnitOfWork.Object, _mockPasswordHasher.Object, null!, _mockMapper.Object, Mock.Of<ILogger<UserService>>()));
    }

    [TestMethod]
    public void Constructor_NullMapper_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new UserService(_mockUnitOfWork.Object, _mockPasswordHasher.Object, _mockJwtTokenService.Object, null!, Mock.Of<ILogger<UserService>>()));
    }
}