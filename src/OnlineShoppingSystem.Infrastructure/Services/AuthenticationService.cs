using AutoMapper;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Commands.User;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Domain.Exceptions;

namespace OnlineShoppingSystem.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public AuthenticationService(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<AuthTokenDto> LoginAsync(LoginCommand command)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(command.Email);
        if (user == null)
        {
            throw new UserNotFoundException($"User with email {command.Email} not found");
        }

        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var token = _jwtTokenService.GenerateToken(user);
        var userDto = _mapper.Map<UserDto>(user);

        return new AuthTokenDto
        {
            Token = token,
            ExpiresAt = _jwtTokenService.GetTokenExpiration(),
            User = userDto
        };
    }

    public async Task<bool> ValidateUserCredentialsAsync(string email, string password)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email);
        if (user == null)
        {
            return false;
        }

        return _passwordHasher.VerifyPassword(password, user.PasswordHash);
    }
}