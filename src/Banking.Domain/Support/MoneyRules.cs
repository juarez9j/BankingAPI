using Banking.Domain.Exceptions;

namespace Banking.Domain.Support;

public static class MoneyRules
{
    public static bool HasAtMostTwoDecimals(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero) == value;

    public static long ToMinorUnits(decimal value, string fieldName)
    {
        if (!HasAtMostTwoDecimals(value))
        {
            throw new AccountValidationException($"{fieldName} admite como máximo dos decimales.");
        }

        decimal scaled = value * 100m;
        if (scaled < long.MinValue || scaled > long.MaxValue)
        {
            throw new AccountValidationException($"{fieldName} está fuera del rango soportado.");
        }

        return decimal.ToInt64(scaled);
    }

    public static decimal FromMinorUnits(long value) => value / 100m;

    public static void EnsureNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
        {
            throw new AccountValidationException($"{fieldName} no puede ser negativo.");
        }
    }

    public static void EnsurePositive(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            throw new AccountValidationException($"{fieldName} debe ser mayor que cero.");
        }
    }
}
