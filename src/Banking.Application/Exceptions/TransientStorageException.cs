namespace Banking.Application.Exceptions;

public sealed class TransientStorageException : ApplicationExceptionBase
{
    public TransientStorageException(string code, string message)
        : base(code, message)
    {
    }
}
