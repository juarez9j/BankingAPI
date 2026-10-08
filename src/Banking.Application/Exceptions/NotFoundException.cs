namespace Banking.Application.Exceptions;

public sealed class NotFoundException : ApplicationExceptionBase
{
    public NotFoundException(string code, string message)
        : base(code, message)
    {
    }
}
