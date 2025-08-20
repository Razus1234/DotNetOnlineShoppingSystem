using OnlineShoppingSystem.Application.Commands.User;
using OnlineShoppingSystem.Application.DTOs;

namespace OnlineShoppingSystem.Application.Common.Interfaces;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterUserCommand command);
    Task<AuthTokenDto> LoginAsync(LoginCommand command);
    Task<UserDto> GetUserProfileAsync(Guid userId);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateUserCommand command);
}