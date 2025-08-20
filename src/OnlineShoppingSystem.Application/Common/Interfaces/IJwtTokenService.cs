using System.Security.Claims;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiration();
}