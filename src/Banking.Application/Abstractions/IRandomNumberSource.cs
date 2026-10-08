namespace Banking.Application.Abstractions;

public interface IRandomNumberSource
{
    int Next(int maxExclusive);
}
