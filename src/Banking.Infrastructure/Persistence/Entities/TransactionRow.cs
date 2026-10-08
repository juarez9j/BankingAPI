namespace Banking.Infrastructure.Persistence.Entities;

public sealed class TransactionRow
{
    public Guid TransactionId { get; set; }
    public Guid AccountId { get; set; }
    public AccountRow Account { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
    public long AmountMinorUnits { get; set; }
    public DateTime TimestampUtc { get; set; }
    public long BalanceAfterMinorUnits { get; set; }
}
