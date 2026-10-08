using Banking.Application.Abstractions;

namespace Banking.Application.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
