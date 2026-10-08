using Banking.Application.Abstractions;

namespace Banking.Application.Services;

public sealed class SystemRandomNumberSource : IRandomNumberSource
{
    public int Next(int maxExclusive) => Random.Shared.Next(maxExclusive);
}
