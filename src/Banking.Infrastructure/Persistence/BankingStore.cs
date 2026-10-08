using Banking.Application.Abstractions;
using Banking.Application.Exceptions;
using Banking.Application.Models;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Domain.Support;
using Banking.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Banking.Infrastructure.Persistence;

public sealed class BankingStore : IBankingStore
{
    private readonly BankingDbContext _db;

    public BankingStore(BankingDbContext db)
    {
        _db = db;
    }

    public Task<bool> ClientExistsAsync(Guid clientId, CancellationToken cancellationToken)
        => _db.Clients.AnyAsync(x => x.Id == clientId, cancellationToken);

    public Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken)
        => _db.Accounts.AnyAsync(x => x.AccountNumber == accountNumber, cancellationToken);

    public async Task<ClientResult> AddClientAsync(Client client, CancellationToken cancellationToken)
    {
        var row = new ClientRow
        {
            Id = client.Id,
            FullName = client.FullName,
            DateOfBirth = client.DateOfBirth,
            Sex = client.Sex,
            MonthlyIncomeMinorUnits = MoneyRules.ToMinorUnits(client.MonthlyIncome, nameof(client.MonthlyIncome)),
            IncomeCurrency = client.IncomeCurrency
        };

        _db.Clients.Add(row);

        await SaveChangesSafeAsync(cancellationToken);
        return ToClientResult(row);
    }

    public async Task<AccountResult> CreateAccountAsync(BankAccount account, BankTransaction? initialDeposit, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var row = new AccountRow
            {
                Id = account.Id,
                ClientId = account.ClientId,
                AccountNumber = account.AccountNumber,
                Currency = account.Currency,
                BalanceMinorUnits = MoneyRules.ToMinorUnits(account.Balance, nameof(account.Balance)),
                CreatedAtUtc = account.CreatedAtUtc
            };

            _db.Accounts.Add(row);

            if (initialDeposit is not null)
            {
                _db.Transactions.Add(new TransactionRow
                {
                    TransactionId = initialDeposit.TransactionId,
                    AccountId = initialDeposit.AccountId,
                    Type = initialDeposit.Type.ToString(),
                    AmountMinorUnits = MoneyRules.ToMinorUnits(initialDeposit.Amount, nameof(initialDeposit.Amount)),
                    TimestampUtc = initialDeposit.TimestampUtc,
                    BalanceAfterMinorUnits = MoneyRules.ToMinorUnits(initialDeposit.BalanceAfter, nameof(initialDeposit.BalanceAfter))
                });
            }

            await SaveChangesSafeAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ToAccountResult(row);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<BalanceResult?> GetBalanceAsync(string accountNumber, CancellationToken cancellationToken)
    {
        var row = await _db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.AccountNumber == accountNumber, cancellationToken);
        return row is null ? null : new BalanceResult(row.ClientId, row.AccountNumber, row.Currency, MoneyRules.FromMinorUnits(row.BalanceMinorUnits));
    }

    public async Task<TransactionResult> DepositAsync(string accountNumber, decimal amount, DateTime timestampUtc, CancellationToken cancellationToken)
    {
        var amountMinorUnits = MoneyRules.ToMinorUnits(amount, nameof(amount));

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Accounts SET BalanceMinorUnits = BalanceMinorUnits + {amountMinorUnits} WHERE AccountNumber = {accountNumber}",
                cancellationToken);

            if (affected == 0)
            {
                throw new NotFoundException("ACCOUNT_NOT_FOUND", "La cuenta no existe.");
            }

            var account = await _db.Accounts.AsNoTracking().SingleAsync(x => x.AccountNumber == accountNumber, cancellationToken);
            var balanceAfter = account.BalanceMinorUnits;
            var transactionRow = new TransactionRow
            {
                TransactionId = Guid.NewGuid(),
                AccountId = account.Id,
                Type = TransactionType.Deposit.ToString(),
                AmountMinorUnits = amountMinorUnits,
                TimestampUtc = timestampUtc,
                BalanceAfterMinorUnits = balanceAfter
            };
            _db.Transactions.Add(transactionRow);
            await SaveChangesSafeAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ToTransactionResult(transactionRow, accountNumber);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<TransactionResult> WithdrawAsync(string accountNumber, decimal amount, DateTime timestampUtc, CancellationToken cancellationToken)
    {
        var amountMinorUnits = MoneyRules.ToMinorUnits(amount, nameof(amount));

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Accounts SET BalanceMinorUnits = BalanceMinorUnits - {amountMinorUnits} WHERE AccountNumber = {accountNumber} AND BalanceMinorUnits >= {amountMinorUnits}",
                cancellationToken);

            if (affected == 0)
            {
                var accountExists = await _db.Accounts.AsNoTracking().AnyAsync(x => x.AccountNumber == accountNumber, cancellationToken);
                if (!accountExists)
                {
                    throw new NotFoundException("ACCOUNT_NOT_FOUND", "La cuenta no existe.");
                }

                throw new InsufficientFundsException();
            }

            var account = await _db.Accounts.AsNoTracking().SingleAsync(x => x.AccountNumber == accountNumber, cancellationToken);
            var balanceAfter = account.BalanceMinorUnits;
            var transactionRow = new TransactionRow
            {
                TransactionId = Guid.NewGuid(),
                AccountId = account.Id,
                Type = TransactionType.Withdrawal.ToString(),
                AmountMinorUnits = amountMinorUnits,
                TimestampUtc = timestampUtc,
                BalanceAfterMinorUnits = balanceAfter
            };
            _db.Transactions.Add(transactionRow);
            await SaveChangesSafeAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ToTransactionResult(transactionRow, accountNumber);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<IReadOnlyList<TransactionResult>> GetTransactionsAsync(string accountNumber, CancellationToken cancellationToken)
    {
        var query = from tr in _db.Transactions.AsNoTracking()
                    join acc in _db.Accounts.AsNoTracking() on tr.AccountId equals acc.Id
                    where acc.AccountNumber == accountNumber
                    orderby tr.TimestampUtc descending, tr.TransactionId descending
                    select new TransactionResult(
                        tr.TransactionId,
                        acc.AccountNumber,
                        tr.Type,
                        MoneyRules.FromMinorUnits(tr.AmountMinorUnits),
                        tr.TimestampUtc,
                        MoneyRules.FromMinorUnits(tr.BalanceAfterMinorUnits));

        return await query.ToListAsync(cancellationToken);
    }

    private async Task SaveChangesSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateAccountNumberException("El número de cuenta ya existe.");
        }
        catch (DbUpdateException ex) when (IsTransientSqlite(ex))
        {
            throw new TransientStorageException("STORAGE_TRANSIENT_CONFLICT", "Se produjo un conflicto transitorio al persistir los datos.");
        }
        catch (SqliteException ex) when (IsTransientSqlite(ex))
        {
            throw new TransientStorageException("STORAGE_TRANSIENT_CONFLICT", "Se produjo un conflicto transitorio al persistir los datos.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is SqliteException sqlite && sqlite.SqliteErrorCode == 19 && sqlite.SqliteExtendedErrorCode == 2067;

    private static bool IsTransientSqlite(Exception ex)
        => ex is SqliteException sqlite && (sqlite.SqliteErrorCode is 5 or 6);

    private static ClientResult ToClientResult(ClientRow row)
        => new(row.Id, row.FullName, row.DateOfBirth, row.Sex, MoneyRules.FromMinorUnits(row.MonthlyIncomeMinorUnits), row.IncomeCurrency);

    private static AccountResult ToAccountResult(AccountRow row)
        => new(row.Id, row.ClientId, row.AccountNumber, row.Currency, MoneyRules.FromMinorUnits(row.BalanceMinorUnits), row.CreatedAtUtc);

    private static TransactionResult ToTransactionResult(TransactionRow row, string accountNumber)
        => new(row.TransactionId, accountNumber, row.Type, MoneyRules.FromMinorUnits(row.AmountMinorUnits), row.TimestampUtc, MoneyRules.FromMinorUnits(row.BalanceAfterMinorUnits));
}
