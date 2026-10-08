using Banking.Domain.Exceptions;

namespace Banking.Domain.Support;

public static class AccountNumberRules
{
    public const string Prefix = "ACC-";

    public static string Build(DateOnly utcDate, int randomDigits)
    {
        if (randomDigits is < 0 or > 9999)
        {
            throw new AccountNumberGenerationException("Los dígitos aleatorios deben estar entre 0000 y 9999.");
        }

        return $"{Prefix}{utcDate:yyyyMMdd}-{randomDigits:0000}";
    }

    public static bool IsValidFormat(string value)
        => value.Length == 17
           && value.StartsWith(Prefix, StringComparison.Ordinal)
           && value[4..12].All(char.IsDigit)
           && value[12] == '-'
           && value[13..17].All(char.IsDigit);
}
