using Banking.Application.Abstractions;
using Banking.Domain.Support;

namespace Banking.Application.Services;

public sealed class AccountNumberGenerator : IAccountNumberGenerator
{
    private readonly IClock _clock;
    private readonly IRandomNumberSource _random;

    public AccountNumberGenerator(IClock clock, IRandomNumberSource random)
    {
        _clock = clock;
        _random = random;
    }

    public string Generate()
    {
        var utcNow = _clock.UtcNow;
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("La fuente de tiempo debe devolver UTC.");
        }

        var digits = _random.Next(10_000);
        return AccountNumberRules.Build(DateOnly.FromDateTime(utcNow), digits);
    }
}
