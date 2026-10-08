using Banking.Application.Abstractions;
using Banking.Application.Exceptions;
using Banking.Application.Models;
using Banking.Domain.Entities;

namespace Banking.Application.Services;

public sealed class CreateAccountService
{
    private const int MaxAccountNumberAttempts = 10;

    private readonly IBankingStore _store;
    private readonly IAccountNumberGenerator _accountNumberGenerator;
    private readonly IClock _clock;

    public CreateAccountService(IBankingStore store, IAccountNumberGenerator accountNumberGenerator, IClock clock)
    {
        _store = store;
        _accountNumberGenerator = accountNumberGenerator;
        _clock = clock;
    }

    public async Task<AccountResult> ExecuteAsync(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        if (!await _store.ClientExistsAsync(command.ClientId, cancellationToken))
        {
            throw new NotFoundException("CLIENT_NOT_FOUND", "El cliente no existe.");
        }

        for (var attempt = 1; attempt <= MaxAccountNumberAttempts; attempt++)
        {
            var accountNumber = _accountNumberGenerator.Generate();
            var createdAtUtc = _clock.UtcNow;
            var account = BankAccount.Open(Guid.NewGuid(), command.ClientId, accountNumber, command.Currency, command.InitialBalance, createdAtUtc);

            BankTransaction? initialDeposit = null;
            if (command.InitialBalance > 0)
            {
                initialDeposit = BankTransaction.Record(Guid.NewGuid(), account.Id, Domain.Enums.TransactionType.Deposit, command.InitialBalance, createdAtUtc, command.InitialBalance);
            }

            try
            {
                return await RetryTransientAsync(() => _store.CreateAccountAsync(account, initialDeposit, cancellationToken));
            }
            catch (DuplicateAccountNumberException) when (attempt < MaxAccountNumberAttempts)
            {
                continue;
            }
            catch (DuplicateAccountNumberException)
            {
                break;
            }
        }

        throw new TransientStorageException("ACCOUNT_NUMBER_GENERATION_FAILED", "No se pudo generar un número de cuenta único.");
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
