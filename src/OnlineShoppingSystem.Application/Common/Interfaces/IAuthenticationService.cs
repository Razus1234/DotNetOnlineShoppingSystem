using OnlineShoppingSystem.Application.Commands.User;
using OnlineShoppingSystem.Application.DTOs;

namespace OnlineShoppingSystem.Application.Common.Interfaces;

public interface IAuthenticationService
{
    Task<AuthTokenDto> LoginAsync(LoginCommand command);
    Task<bool> ValidateUserCredentialsAsync(string email, string password);
}