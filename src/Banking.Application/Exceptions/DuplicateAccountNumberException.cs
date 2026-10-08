namespace Banking.Application.Exceptions;

public sealed class DuplicateAccountNumberException : ApplicationExceptionBase
{
    public DuplicateAccountNumberException(string message)
        : base("DUPLICATE_ACCOUNT_NUMBER", message)
    {
    }
}
