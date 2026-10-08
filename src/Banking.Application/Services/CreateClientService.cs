using Banking.Application.Abstractions;
using Banking.Application.Exceptions;
using Banking.Application.Models;
using Banking.Domain.Entities;

namespace Banking.Application.Services;

public sealed class CreateClientService
{
    private readonly IBankingStore _store;

    public CreateClientService(IBankingStore store)
    {
        _store = store;
    }

    public async Task<ClientResult> ExecuteAsync(CreateClientCommand command, CancellationToken cancellationToken)
    {
        var client = Client.Create(Guid.NewGuid(), command.FullName, command.DateOfBirth, command.Sex, command.MonthlyIncome, command.IncomeCurrency);
        return await RetryTransientAsync(() => _store.AddClientAsync(client, cancellationToken));
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
