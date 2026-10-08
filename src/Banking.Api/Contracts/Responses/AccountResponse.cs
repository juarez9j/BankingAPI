namespace Banking.Api.Contracts.Responses;

public sealed record AccountResponse(Guid Id, Guid ClientId, string AccountNumber, string Currency, decimal Balance, DateTime CreatedAtUtc);
