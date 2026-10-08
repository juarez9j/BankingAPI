using Banking.Domain.Exceptions;
using Banking.Domain.Support;

namespace Banking.Domain.Entities;

public sealed class Client
{
    private Client()
    {
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public string Sex { get; private set; } = string.Empty;
    public decimal MonthlyIncome { get; private set; }
    public string IncomeCurrency { get; private set; } = string.Empty;

    public static Client Create(Guid id, string fullName, DateOnly dateOfBirth, string sex, decimal monthlyIncome, string incomeCurrency)
    {
        if (id == Guid.Empty)
        {
            throw new ClientValidationException("El identificador del cliente es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ClientValidationException("El nombre completo es obligatorio.");
        }

        if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ClientValidationException("La fecha de nacimiento no puede ser futura.");
        }

        var normalizedSex = sex?.Trim().ToUpperInvariant();
        if (normalizedSex is not ("M" or "F"))
        {
            throw new ClientValidationException("Sex solo admite M o F.");
        }

        MoneyRules.EnsureNonNegative(monthlyIncome, nameof(monthlyIncome));
        if (!MoneyRules.HasAtMostTwoDecimals(monthlyIncome))
        {
            throw new ClientValidationException("MonthlyIncome admite como máximo dos decimales.");
        }

        var normalizedCurrency = incomeCurrency?.Trim().ToUpperInvariant();
        if (normalizedCurrency is not ("USD" or "NIO"))
        {
            throw new ClientValidationException("IncomeCurrency solo admite USD o NIO.");
        }

        return new Client
        {
            Id = id,
            FullName = fullName.Trim(),
            DateOfBirth = dateOfBirth,
            Sex = normalizedSex,
            MonthlyIncome = monthlyIncome,
            IncomeCurrency = normalizedCurrency
        };
    }
}
