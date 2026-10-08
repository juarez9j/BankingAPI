using Banking.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence;

public sealed class BankingDbContext : DbContext
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClientRow> Clients => Set<ClientRow>();
    public DbSet<AccountRow> Accounts => Set<AccountRow>();
    public DbSet<TransactionRow> Transactions => Set<TransactionRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientRow>(entity =>
        {
            entity.ToTable("Clients");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).IsRequired();
            entity.Property(x => x.Sex).IsRequired().HasMaxLength(1);
            entity.Property(x => x.IncomeCurrency).IsRequired().HasMaxLength(3);
            entity.Property(x => x.MonthlyIncomeMinorUnits).IsRequired();
        });

        modelBuilder.Entity<AccountRow>(entity =>
        {
            entity.ToTable("Accounts");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.AccountNumber).IsUnique();
            entity.Property(x => x.AccountNumber).IsRequired().HasMaxLength(17);
            entity.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            entity.Property(x => x.BalanceMinorUnits).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasOne(x => x.Client)
                .WithMany(x => x.Accounts)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TransactionRow>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(x => x.TransactionId);
            entity.Property(x => x.Type).IsRequired().HasMaxLength(20);
            entity.Property(x => x.AmountMinorUnits).IsRequired();
            entity.Property(x => x.TimestampUtc).IsRequired();
            entity.Property(x => x.BalanceAfterMinorUnits).IsRequired();
            entity.HasOne(x => x.Account)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
