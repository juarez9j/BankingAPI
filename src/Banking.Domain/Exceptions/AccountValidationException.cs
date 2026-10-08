namespace Banking.Domain.Exceptions;

public sealed class AccountValidationException : DomainException
{
    public AccountValidationException(string message)
        : base("INVALID_ACCOUNT_DATA", message)
    {
    }
}
