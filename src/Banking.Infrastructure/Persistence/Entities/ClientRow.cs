namespace Banking.Infrastructure.Persistence.Entities;

public sealed class ClientRow
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Sex { get; set; } = string.Empty;
    public long MonthlyIncomeMinorUnits { get; set; }
    public string IncomeCurrency { get; set; } = string.Empty;

    public List<AccountRow> Accounts { get; set; } = new();
}
