namespace Banking.Application.Models;

public sealed record ClientResult(Guid Id, string FullName, DateOnly DateOfBirth, string Sex, decimal MonthlyIncome, string IncomeCurrency);

public sealed record AccountResult(Guid Id, Guid ClientId, string AccountNumber, string Currency, decimal Balance, DateTime CreatedAtUtc);

public sealed record BalanceResult(Guid ClientId, string AccountNumber, string Currency, decimal Balance);

public sealed record TransactionResult(Guid TransactionId, string AccountNumber, string Type, decimal Amount, DateTime TimestampUtc, decimal BalanceAfter);
