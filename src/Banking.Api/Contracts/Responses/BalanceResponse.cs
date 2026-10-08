namespace Banking.Api.Contracts.Responses;

public sealed record BalanceResponse(Guid ClientId, string AccountNumber, string Currency, decimal Balance);
