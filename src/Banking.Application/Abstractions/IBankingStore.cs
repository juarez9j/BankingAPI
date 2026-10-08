using Banking.Application.Models;
using Banking.Domain.Entities;

namespace Banking.Application.Abstractions;

public interface IBankingStore
{
    Task<bool> ClientExistsAsync(Guid clientId, CancellationToken cancellationToken);
    Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken);
    Task<ClientResult> AddClientAsync(Client client, CancellationToken cancellationToken);
    Task<AccountResult> CreateAccountAsync(BankAccount account, BankTransaction? initialDeposit, CancellationToken cancellationToken);
    Task<BalanceResult?> GetBalanceAsync(string accountNumber, CancellationToken cancellationToken);
    Task<TransactionResult> DepositAsync(string accountNumber, decimal amount, DateTime timestampUtc, CancellationToken cancellationToken);
    Task<TransactionResult> WithdrawAsync(string accountNumber, decimal amount, DateTime timestampUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionResult>> GetTransactionsAsync(string accountNumber, CancellationToken cancellationToken);
}
