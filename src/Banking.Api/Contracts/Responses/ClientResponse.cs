namespace Banking.Api.Contracts.Responses;

public sealed record ClientResponse(Guid Id, string FullName, DateOnly DateOfBirth, string Sex, decimal MonthlyIncome, string IncomeCurrency);
