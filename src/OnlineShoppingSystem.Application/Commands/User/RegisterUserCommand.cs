using OnlineShoppingSystem.Application.Common.Attributes;
using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSystem.Application.Commands.User;

public class RegisterUserCommand
{
    [Required]
    [SecureEmail]
    [StringLength(254, MinimumLength = 5)]
    [NoInjection]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StrongPassword(MinLength = 8)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    [NoInjection]
    public string FullName { get; set; } = string.Empty;
}