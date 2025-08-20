using OnlineShoppingSystem.Application.Common.Attributes;
using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSystem.Application.Commands.User;

public class LoginCommand
{
    [Required]
    [SecureEmail]
    [NoInjection]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}