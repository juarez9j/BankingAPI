using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Banking.Infrastructure.Migrations;

[DbContext(typeof(BankingDbContext))]
public partial class BankingDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12");

        modelBuilder.Entity("Banking.Infrastructure.Persistence.Entities.ClientRow", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever();
            b.Property<DateOnly>("DateOfBirth");
            b.Property<string>("FullName").IsRequired();
            b.Property<string>("IncomeCurrency").IsRequired().HasMaxLength(3);
            b.Property<long>("MonthlyIncomeMinorUnits");
            b.Property<string>("Sex").IsRequired().HasMaxLength(1);
            b.HasKey("Id");
            b.ToTable("Clients");
        });

        modelBuilder.Entity("Banking.Infrastructure.Persistence.Entities.AccountRow", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever();
            b.Property<string>("AccountNumber").IsRequired().HasMaxLength(17);
            b.Property<long>("BalanceMinorUnits");
            b.Property<Guid>("ClientId");
            b.Property<string>("Currency").IsRequired().HasMaxLength(3);
            b.Property<DateTime>("CreatedAtUtc");
            b.HasKey("Id");
            b.HasIndex("AccountNumber").IsUnique();
            b.HasIndex("ClientId");
            b.ToTable("Accounts");
        });

        modelBuilder.Entity("Banking.Infrastructure.Persistence.Entities.TransactionRow", b =>
        {
            b.Property<Guid>("TransactionId").ValueGeneratedNever();
            b.Property<Guid>("AccountId");
            b.Property<long>("AmountMinorUnits");
            b.Property<long>("BalanceAfterMinorUnits");
            b.Property<string>("Type").IsRequired().HasMaxLength(20);
            b.Property<DateTime>("TimestampUtc");
            b.HasKey("TransactionId");
            b.HasIndex("AccountId");
            b.ToTable("Transactions");
        });

        modelBuilder.Entity("Banking.Infrastructure.Persistence.Entities.AccountRow", b =>
        {
            b.HasOne("Banking.Infrastructure.Persistence.Entities.ClientRow", "Client")
                .WithMany("Accounts")
                .HasForeignKey("ClientId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Client");
        });

        modelBuilder.Entity("Banking.Infrastructure.Persistence.Entities.TransactionRow", b =>
        {
            b.HasOne("Banking.Infrastructure.Persistence.Entities.AccountRow", "Account")
                .WithMany("Transactions")
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Account");
        });

        modelBuilder.Entity("Banking.Infrastructure.Persistence.Entities.ClientRow", b =>
        {
            b.Navigation("Accounts");
        });

        modelBuilder.Entity("Banking.Infrastructure.Persistence.Entities.AccountRow", b =>
        {
            b.Navigation("Transactions");
        });
    }
}
