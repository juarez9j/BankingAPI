using Banking.Application.Abstractions;
using Banking.Application.Exceptions;
using Banking.Application.Models;

namespace Banking.Application.Services;

public sealed class DepositService
{
    private readonly IBankingStore _store;
    private readonly IClock _clock;

    public DepositService(IBankingStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<TransactionResult> ExecuteAsync(string accountNumber, MoneyCommand command, CancellationToken cancellationToken)
    {
        return await RetryTransientAsync(() => _store.DepositAsync(accountNumber, command.Amount, _clock.UtcNow, cancellationToken));
    }

    private static async Task<T> RetryTransientAsync<T>(Func<Task<T>> action)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (TransientStorageException) when (attempt < maxAttempts)
            {
                await Task.Delay(25);
            }
        }
    }
}
