namespace Banking.Domain.Exceptions;

public sealed class AccountNumberGenerationException : DomainException
{
    public AccountNumberGenerationException(string message)
        : base("ACCOUNT_NUMBER_GENERATION_FAILED", message)
    {
    }
}
