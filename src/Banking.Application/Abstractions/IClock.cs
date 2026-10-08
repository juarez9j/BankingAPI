namespace Banking.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
