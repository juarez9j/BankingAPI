namespace Banking.Api.Contracts.Responses;

public sealed record TransactionResponse(Guid TransactionId, string AccountNumber, string Type, decimal Amount, DateTime TimestampUtc, decimal BalanceAfter);
