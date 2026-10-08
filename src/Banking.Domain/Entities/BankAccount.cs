using Banking.Domain.Exceptions;
using Banking.Domain.Support;
using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

public sealed class BankAccount
{
    private BankAccount()
    {
    }

    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static BankAccount Open(Guid id, Guid clientId, string accountNumber, string currency, decimal initialBalance, DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new AccountValidationException("El identificador de cuenta es obligatorio.");
        }

        if (clientId == Guid.Empty)
        {
            throw new AccountValidationException("ClientId es obligatorio.");
        }

        if (!AccountNumberRules.IsValidFormat(accountNumber))
        {
            throw new AccountValidationException("AccountNumber no cumple el formato requerido.");
        }

        var normalizedCurrency = currency?.Trim().ToUpperInvariant();
        if (normalizedCurrency is not ("USD" or "NIO"))
        {
            throw new AccountValidationException("Currency solo admite USD o NIO.");
        }

        MoneyRules.EnsureNonNegative(initialBalance, nameof(initialBalance));
        if (!MoneyRules.HasAtMostTwoDecimals(initialBalance))
        {
            throw new AccountValidationException("InitialBalance admite como máximo dos decimales.");
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new AccountValidationException("CreatedAtUtc debe estar en UTC.");
        }

        return new BankAccount
        {
            Id = id,
            ClientId = clientId,
            AccountNumber = accountNumber,
            Currency = normalizedCurrency,
            Balance = initialBalance,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0 || !MoneyRules.HasAtMostTwoDecimals(amount))
        {
            throw new AccountValidationException("El depósito debe ser positivo y con máximo dos decimales.");
        }

        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0 || !MoneyRules.HasAtMostTwoDecimals(amount))
        {
            throw new AccountValidationException("El retiro debe ser positivo y con máximo dos decimales.");
        }

        if (amount > Balance)
        {
            throw new InsufficientFundsException();
        }

        Balance -= amount;
    }
}
