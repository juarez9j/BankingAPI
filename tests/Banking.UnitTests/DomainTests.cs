using Banking.Application.Abstractions;
using Banking.Application.Exceptions;
using Banking.Application.Models;
using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Exceptions;
using Banking.Domain.Support;

namespace Banking.UnitTests;

public sealed class DomainTests
{
    [Fact]
    public void CreateClient_RejectsEmptyName()
    {
        // Evalúa validación de nombre vacío en dominio.
        Assert.Throws<ClientValidationException>(() => Client.Create(Guid.NewGuid(), " ", new DateOnly(1990, 1, 1), "M", 100m, "USD"));
    }

    [Fact]
    public void CreateClient_NormalizesSex()
    {
        // Evalúa normalización de sexo y uso puro de dominio, sin mocks ni SQLite.
        var client = Client.Create(Guid.NewGuid(), "Ana López", new DateOnly(1990, 1, 1), "f", 100m, "nio");

        // Verifica que el sexo quede normalizado a mayúscula.
        Assert.Equal("F", client.Sex);
    }

    [Fact]
    public void CreateClient_ValidatesCurrency()
    {
        // Evalúa rechazo de moneda inválida en dominio.
        Assert.Throws<ClientValidationException>(() => Client.Create(Guid.NewGuid(), "Ana López", new DateOnly(1990, 1, 1), "F", 100m, "EUR"));
    }

    [Fact]
    public void CreateClient_RejectsFutureDateOfBirth()
    {
        // Evalúa rechazo de fecha futura en dominio.
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        // Verifica que una fecha futura no sea aceptada.
        Assert.Throws<ClientValidationException>(() => Client.Create(Guid.NewGuid(), "Ana López", futureDate, "F", 100m, "USD"));
    }

    [Fact]
    public void CreateAccount_AllowsZeroInitialBalance()
    {
        // Evalúa apertura de cuenta con saldo inicial cero, sin mocks ni SQLite.
        var account = BankAccount.Open(Guid.NewGuid(), Guid.NewGuid(), "ACC-20261007-0048", "USD", 0m, DateTime.UtcNow);

        // Verifica que el saldo inicial cero se preserve.
        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void CreateAccount_AllowsPositiveInitialBalance()
    {
        // Evalúa apertura de cuenta con saldo inicial positivo, sin mocks ni SQLite.
        var account = BankAccount.Open(Guid.NewGuid(), Guid.NewGuid(), "ACC-20261007-0048", "USD", 150.25m, DateTime.UtcNow);

        // Verifica que el saldo inicial positivo se preserve.
        Assert.Equal(150.25m, account.Balance);
    }

    [Fact]
    public void CreateAccount_RejectsNegativeInitialBalance()
    {
        // Evalúa rechazo de saldo inicial negativo.
        Assert.Throws<AccountValidationException>(() => BankAccount.Open(Guid.NewGuid(), Guid.NewGuid(), "ACC-20261007-0048", "USD", -1m, DateTime.UtcNow));
    }

    [Fact]
    public void Deposit_AllowsValidDeposit()
    {
        // Evalúa depósito válido sobre entidad de dominio, sin mocks ni SQLite.
        var account = BankAccount.Open(Guid.NewGuid(), Guid.NewGuid(), "ACC-20261007-0048", "USD", 10m, DateTime.UtcNow);

        // Ejecuta el depósito.
        account.Deposit(5.75m);

        // Verifica el nuevo saldo.
        Assert.Equal(15.75m, account.Balance);
    }

    [Fact]
    public void Deposit_RejectsZero()
    {
        // Evalúa rechazo de depósito cero.
        var account = BankAccount.Open(Guid.NewGuid(), Guid.NewGuid(), "ACC-20261007-0048", "USD", 10m, DateTime.UtcNow);

        // Verifica que cero no sea aceptado.
        Assert.Throws<AccountValidationException>(() => account.Deposit(0m));
    }

    [Fact]
    public void Deposit_RejectsNegative()
    {
        // Evalúa rechazo de depósito negativo.
        var account = BankAccount.Open(Guid.NewGuid(), Guid.NewGuid(), "ACC-20261007-0048", "USD", 10m, DateTime.UtcNow);

        // Verifica que un monto negativo no sea aceptado.
        Assert.Throws<AccountValidationException>(() => account.Deposit(-1m));
    }

    [Fact]
    public void Withdraw_AllowsValidWithdrawal()
    {
        // Evalúa retiro válido sobre entidad de dominio, sin mocks ni SQLite.
        var account = BankAccount.Open(Guid.NewGuid(), Guid.NewGuid(), "ACC-20261007-0048", "USD", 10m, DateTime.UtcNow);

        // Ejecuta el retiro.
        account.Withdraw(4.25m);

        // Verifica el nuevo saldo.
        Assert.Equal(5.75m, account.Balance);
    }

    [Fact]
    public void Withdraw_RejectsInsufficientFunds()
    {
        // Evalúa rechazo por fondos insuficientes.
        var account = BankAccount.Open(Guid.NewGuid(), Guid.NewGuid(), "ACC-20261007-0048", "USD", 10m, DateTime.UtcNow);

        // Verifica que el dominio bloquee el sobregiro.
        Assert.Throws<InsufficientFundsException>(() => account.Withdraw(10.01m));
    }

    [Fact]
    public void MoneyRules_PreservesPrecision()
    {
        // Evalúa conversión exacta a unidades mínimas, sin mocks ni SQLite.
        var minorUnits = MoneyRules.ToMinorUnits(1234.56m, "amount");

        // Verifica que 1234.56 se represente como 123456.
        Assert.Equal(123456, minorUnits);

        // Verifica la reconversión exacta a decimal.
        Assert.Equal(1234.56m, MoneyRules.FromMinorUnits(minorUnits));
    }

    [Fact]
    public void AccountNumberRules_BuildsExpectedFormat()
    {
        // Evalúa el formato obligatorio del número de cuenta.
        var value = AccountNumberRules.Build(new DateOnly(2026, 10, 7), 48);

        // Verifica el formato completo esperado.
        Assert.Equal("ACC-20261007-0048", value);
    }

    [Fact]
    public void AccountNumberRules_UsesUtcDate()
    {
        // Evalúa que el número de cuenta incorpore la fecha UTC recibida.
        var value = AccountNumberRules.Build(new DateOnly(2026, 10, 7), 1);

        // Verifica que la fecha UTC aparezca en el identificador.
        Assert.Contains("20261007", value);
    }

    [Fact]
    public void AccountNumberRules_KeepsLeadingZeros()
    {
        // Evalúa el relleno con ceros a la izquierda.
        var value = AccountNumberRules.Build(new DateOnly(2026, 10, 7), 7);

        // Verifica que los dígitos se formateen a cuatro posiciones.
        Assert.EndsWith("-0007", value);
    }

    [Fact]
    public async Task CreateAccountService_RetriesOnCollision()
    {
        // Evalúa reintentos del servicio usando un mock/fake de almacenamiento, sin SQLite.
        var store = new FakeStore
        {
            ClientExists = true,
            FailuresBeforeSuccess = 2,
            Result = new AccountResult(Guid.NewGuid(), Guid.NewGuid(), "ACC-20261007-0048", "USD", 0m, new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc))
        };

        // Crea el servicio con generador y reloj controlados.
        var service = new CreateAccountService(store, new FixedAccountNumberGenerator("ACC-20261007-0048"), new FixedClock(new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc)));

        // Ejecuta la creación de cuenta con reintentos por colisión.
        var result = await service.ExecuteAsync(new CreateAccountCommand(Guid.NewGuid(), "USD", 0m), CancellationToken.None);

        // Verifica que se obtuvo el número esperado.
        Assert.Equal("ACC-20261007-0048", result.AccountNumber);

        // Verifica que se realizaron tres intentos totales.
        Assert.Equal(3, store.CreateAttempts);
    }

    [Fact]
    public async Task CreateAccountService_FailsAfterExhaustingRetries()
    {
        // Evalúa el fallo controlado luego de agotar reintentos, usando fake en memoria.
        var store = new FakeStore
        {
            ClientExists = true,
            FailuresBeforeSuccess = int.MaxValue
        };

        // Crea el servicio con fuente de tiempo y cuenta controladas.
        var service = new CreateAccountService(store, new FixedAccountNumberGenerator("ACC-20261007-0048"), new FixedClock(new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc)));

        // Verifica que al agotar reintentos se lance un error de indisponibilidad transitoria.
        await Assert.ThrowsAsync<TransientStorageException>(() => service.ExecuteAsync(new CreateAccountCommand(Guid.NewGuid(), "USD", 0m), CancellationToken.None));

        // Verifica que se alcanzó el máximo de intentos previstos.
        Assert.Equal(10, store.CreateAttempts);
    }

    // Reloj fijo para pruebas unitarias sin dependencia del sistema.
    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    // Generador fijo de número de cuenta para pruebas unitarias.
    private sealed class FixedAccountNumberGenerator : IAccountNumberGenerator
    {
        private readonly string _value;

        public FixedAccountNumberGenerator(string value) => _value = value;

        public string Generate() => _value;
    }

    // Fake de store para simular colisiones y fallos sin SQLite.
    private sealed class FakeStore : IBankingStore
    {
        public bool ClientExists { get; set; }
        public int FailuresBeforeSuccess { get; set; }
        public int CreateAttempts { get; private set; }
        public AccountResult? Result { get; set; }

        // Simula la verificación de cliente existente.
        public Task<bool> ClientExistsAsync(Guid clientId, CancellationToken cancellationToken) => Task.FromResult(ClientExists);

        // No se usa en estas pruebas unitarias.
        public Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken) => Task.FromResult(false);

        // No se usa en estas pruebas unitarias.
        public Task<ClientResult> AddClientAsync(Client client, CancellationToken cancellationToken) => throw new NotImplementedException();

        // Simula colisiones y éxito posterior en la creación de cuenta.
        public Task<AccountResult> CreateAccountAsync(BankAccount account, BankTransaction? initialDeposit, CancellationToken cancellationToken)
        {
            CreateAttempts++;

            // Simula fallo por duplicado durante los primeros intentos.
            if (CreateAttempts <= FailuresBeforeSuccess)
            {
                throw new DuplicateAccountNumberException("duplicado");
            }

            // Devuelve el resultado simulado al superar el umbral.
            return Task.FromResult(Result ?? new AccountResult(account.Id, account.ClientId, account.AccountNumber, account.Currency, account.Balance, account.CreatedAtUtc));
        }

        // No se usa en estas pruebas unitarias.
        public Task<BalanceResult?> GetBalanceAsync(string accountNumber, CancellationToken cancellationToken) => throw new NotImplementedException();

        // No se usa en estas pruebas unitarias.
        public Task<TransactionResult> DepositAsync(string accountNumber, decimal amount, DateTime timestampUtc, CancellationToken cancellationToken) => throw new NotImplementedException();

        // No se usa en estas pruebas unitarias.
        public Task<TransactionResult> WithdrawAsync(string accountNumber, decimal amount, DateTime timestampUtc, CancellationToken cancellationToken) => throw new NotImplementedException();

        // No se usa en estas pruebas unitarias.
        public Task<IReadOnlyList<TransactionResult>> GetTransactionsAsync(string accountNumber, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
