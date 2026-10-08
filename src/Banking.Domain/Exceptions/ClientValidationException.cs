namespace Banking.Domain.Exceptions;

public sealed class ClientValidationException : DomainException
{
    public ClientValidationException(string message)
        : base("INVALID_CLIENT_DATA", message)
    {
    }
}
