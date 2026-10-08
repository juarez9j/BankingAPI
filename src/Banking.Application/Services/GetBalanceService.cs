using Banking.Application.Abstractions;
using Banking.Application.Exceptions;
using Banking.Application.Models;

namespace Banking.Application.Services;

public sealed class GetBalanceService
{
    private readonly IBankingStore _store;

    public GetBalanceService(IBankingStore store)
    {
        _store = store;
    }

    public async Task<BalanceResult> ExecuteAsync(string accountNumber, CancellationToken cancellationToken)
    {
        var balance = await _store.GetBalanceAsync(accountNumber, cancellationToken)
            ?? throw new NotFoundException("ACCOUNT_NOT_FOUND", "La cuenta no existe.");

        return balance;
    }
}
