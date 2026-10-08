using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Domain.Support;

namespace Banking.Domain.Entities;

public sealed class BankTransaction
{
    private BankTransaction()
    {
    }

    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public decimal BalanceAfter { get; private set; }

    public static BankTransaction Record(Guid transactionId, Guid accountId, TransactionType type, decimal amount, DateTime timestampUtc, decimal balanceAfter)
    {
        if (transactionId == Guid.Empty)
        {
            throw new AccountValidationException("TransactionId es obligatorio.");
        }

        if (accountId == Guid.Empty)
        {
            throw new AccountValidationException("AccountId es obligatorio.");
        }

        if (!MoneyRules.HasAtMostTwoDecimals(amount) || amount <= 0)
        {
            throw new AccountValidationException("Amount debe ser positivo y admitir como máximo dos decimales.");
        }

        if (timestampUtc.Kind != DateTimeKind.Utc)
        {
            throw new AccountValidationException("TimestampUtc debe estar en UTC.");
        }

        if (!MoneyRules.HasAtMostTwoDecimals(balanceAfter))
        {
            throw new AccountValidationException("BalanceAfter admite como máximo dos decimales.");
        }

        return new BankTransaction
        {
            TransactionId = transactionId,
            AccountId = accountId,
            Type = type,
            Amount = amount,
            TimestampUtc = timestampUtc,
            BalanceAfter = balanceAfter
        };
    }
}
