using Banking.Application.Abstractions;
using Banking.Application.Exceptions;
using Banking.Application.Models;

namespace Banking.Application.Services;

public sealed class GetTransactionsService
{
    private readonly IBankingStore _store;

    public GetTransactionsService(IBankingStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<TransactionResult>> ExecuteAsync(string accountNumber, CancellationToken cancellationToken)
    {
        var transactions = await _store.GetTransactionsAsync(accountNumber, cancellationToken);
        if (transactions.Count == 0)
        {
            var balance = await _store.GetBalanceAsync(accountNumber, cancellationToken);
            if (balance is null)
            {
                throw new NotFoundException("ACCOUNT_NOT_FOUND", "La cuenta no existe.");
            }
        }

        return transactions;
    }
}
