using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSystem.Application.Commands.User;

public class UpdateUserCommand
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;
}