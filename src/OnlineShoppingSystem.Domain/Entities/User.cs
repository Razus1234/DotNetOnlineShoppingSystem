using OnlineShoppingSystem.Domain.Common;
using OnlineShoppingSystem.Domain.ValueObjects;
using System.Text.RegularExpressions;

namespace OnlineShoppingSystem.Domain.Entities;

public class User : BaseEntity
{
    private readonly List<Address> _addresses = new();
    
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public IReadOnlyList<Address> Addresses => _addresses.AsReadOnly();
    public Cart? Cart { get; private set; }

    private User() // For EF Core
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        FullName = string.Empty;
    }

    public User(string email, string passwordHash, string fullName)
    {
        ValidateEmail(email);
        ValidatePasswordHash(passwordHash);
        ValidateFullName(fullName);

        Email = email.ToLowerInvariant().Trim();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
    }

    public static User Create(string email, string passwordHash, string fullName)
    {
        var user = new User(email, passwordHash, fullName);
        user.CreateCart();
        return user;
    }

    public void AssignCart(Cart cart)
    {
        if (cart == null)
            throw new ArgumentNullException(nameof(cart));

        if (cart.UserId != Id)
            throw new InvalidOperationException("Cart does not belong to this user");

        Cart = cart;
        UpdateTimestamp();
    }

    public void UpdateProfile(string fullName)
    {
        ValidateFullName(fullName);
        FullName = fullName.Trim();
        UpdateTimestamp();
    }

    public void ChangePassword(string newPasswordHash)
    {
        ValidatePasswordHash(newPasswordHash);
        PasswordHash = newPasswordHash;
        UpdateTimestamp();
    }

    public void AddAddress(Address address)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        if (_addresses.Contains(address))
            throw new InvalidOperationException("Address already exists for this user");

        _addresses.Add(address);
        UpdateTimestamp();
    }

    public void RemoveAddress(Address address)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        if (!_addresses.Remove(address))
            throw new InvalidOperationException("Address not found for this user");

        UpdateTimestamp();
    }

    public void CreateCart()
    {
        if (Cart != null)
            throw new InvalidOperationException("User already has a cart");

        Cart = new Cart(Id);
        UpdateTimestamp();
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));

        var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        if (!emailRegex.IsMatch(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (email.Length > 254)
            throw new ArgumentException("Email cannot exceed 254 characters", nameof(email));
    }

    private static void ValidatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be null or empty", nameof(passwordHash));

        if (passwordHash.Length < 60) // BCrypt hash is typically 60 characters
            throw new ArgumentException("Password hash appears to be invalid", nameof(passwordHash));
    }

    private static void ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be null or empty", nameof(fullName));

        if (fullName.Trim().Length < 2)
            throw new ArgumentException("Full name must be at least 2 characters long", nameof(fullName));

        if (fullName.Length > 100)
            throw new ArgumentException("Full name cannot exceed 100 characters", nameof(fullName));
    }
}