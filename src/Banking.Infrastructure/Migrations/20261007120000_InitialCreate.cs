using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Banking.Infrastructure.Migrations;

[Migration("20261007120000_InitialCreate")]
[DbContext(typeof(BankingDbContext))]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Clients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                FullName = table.Column<string>(type: "TEXT", nullable: false),
                DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Sex = table.Column<string>(type: "TEXT", maxLength: 1, nullable: false),
                MonthlyIncomeMinorUnits = table.Column<long>(type: "INTEGER", nullable: false),
                IncomeCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Clients", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Accounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ClientId = table.Column<Guid>(type: "TEXT", nullable: false),
                AccountNumber = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                BalanceMinorUnits = table.Column<long>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Accounts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Accounts_Clients_ClientId",
                    column: x => x.ClientId,
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Transactions",
            columns: table => new
            {
                TransactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                AmountMinorUnits = table.Column<long>(type: "INTEGER", nullable: false),
                TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                BalanceAfterMinorUnits = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                table.ForeignKey(
                    name: "FK_Transactions_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_AccountNumber",
            table: "Accounts",
            column: "AccountNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_ClientId",
            table: "Accounts",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_AccountId",
            table: "Transactions",
            column: "AccountId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Transactions");
        migrationBuilder.DropTable(name: "Accounts");
        migrationBuilder.DropTable(name: "Clients");
    }
}
