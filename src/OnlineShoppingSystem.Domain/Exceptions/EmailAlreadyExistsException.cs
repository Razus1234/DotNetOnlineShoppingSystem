namespace OnlineShoppingSystem.Domain.Exceptions;

public class EmailAlreadyExistsException : DomainException
{
    public EmailAlreadyExistsException(string email) 
        : base($"User with email '{email}' already exists")
    {
    }
}