namespace Banking.Domain.Exceptions;

public sealed class InsufficientFundsException : DomainException
{
    public InsufficientFundsException()
        : base("INSUFFICIENT_FUNDS", "La cuenta no dispone de fondos suficientes.")
    {
    }
}
