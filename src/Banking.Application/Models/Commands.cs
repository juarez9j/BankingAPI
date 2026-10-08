namespace Banking.Application.Models;

public sealed record CreateClientCommand(string FullName, DateOnly DateOfBirth, string Sex, decimal MonthlyIncome, string IncomeCurrency);

public sealed record CreateAccountCommand(Guid ClientId, string Currency, decimal InitialBalance);

public sealed record MoneyCommand(decimal Amount);
