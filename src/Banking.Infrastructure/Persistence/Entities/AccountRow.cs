namespace Banking.Infrastructure.Persistence.Entities;

public sealed class AccountRow
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public ClientRow Client { get; set; } = null!;
    public string AccountNumber { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public long BalanceMinorUnits { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<TransactionRow> Transactions { get; set; } = new();
}
