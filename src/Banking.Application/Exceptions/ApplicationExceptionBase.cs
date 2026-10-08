namespace Banking.Application.Exceptions;

public abstract class ApplicationExceptionBase : Exception
{
    protected ApplicationExceptionBase(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
