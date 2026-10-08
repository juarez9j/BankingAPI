using System.Net;
using System.Net.Http.Json;
using Banking.Application.Abstractions;
using Banking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Banking.IntegrationTests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task CreateClient_PersistsClient()
    {
        // Integra API + SQLite real para validar persistencia de cliente.
        using var harness = await CreateHarnessAsync();

        // Envía request HTTP real al endpoint de creación de clientes.
        var response = await harness.Client.PostAsJsonAsync("/api/clients", new
        {
            fullName = "Ana López",
            dateOfBirth = "1995-04-12",
            sex = "F",
            monthlyIncome = 25000.00m,
            incomeCurrency = "NIO"
        });

        // Verifica código HTTP de creación exitosa.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Lee el DTO de respuesta del cliente.
        var body = await response.Content.ReadFromJsonAsync<ClientResponseDto>();

        // Verifica que el cuerpo no sea nulo.
        Assert.NotNull(body);

        // Verifica que el identificador se haya generado.
        Assert.NotEqual(Guid.Empty, body!.Id);
    }

    [Fact]
    public async Task CreateAccount_PersistsWithInitialDeposit()
    {
        // Integra API + SQLite real para validar apertura de cuenta con depósito inicial.
        using var harness = await CreateHarnessAsync();

        // Crea un cliente vía API para usarlo como dueño de la cuenta.
        var clientId = await CreateClientAsync(harness.Client);

        // Envía request HTTP real al endpoint de cuentas.
        var response = await harness.Client.PostAsJsonAsync("/api/accounts", new
        {
            clientId,
            currency = "USD",
            initialBalance = 150.00m
        });

        // Verifica código HTTP de creación exitosa.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Lee el DTO de respuesta de cuenta.
        var account = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        // Verifica que el cuerpo no sea nulo.
        Assert.NotNull(account);

        // Consulta el saldo por el endpoint correspondiente.
        var balance = await harness.Client.GetFromJsonAsync<BalanceResponseDto>($"/api/accounts/{account!.AccountNumber}/balance");

        // Verifica que el saldo inicial persistido sea correcto.
        Assert.Equal(150.0m, balance!.Balance);

        // Consulta el historial de movimientos.
        var transactions = await harness.Client.GetFromJsonAsync<List<TransactionResponseDto>>($"/api/accounts/{account.AccountNumber}/transactions");

        // Verifica que el depósito inicial quede registrado como un movimiento.
        Assert.Single(transactions!);
    }

    [Fact]
    public async Task UniqueAccountNumberConstraint_RejectsDuplicates()
    {
        // Integra directamente con SQLite real para validar el índice UNIQUE físico.
        using var harness = await CreateHarnessAsync();

        // Abre un scope de servicios para manipular el DbContext directamente.
        using var scope = harness.Factory.Services.CreateScope();

        // Obtiene el contexto real de EF Core contra SQLite.
        var db = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        // Prepara un cliente persistido manualmente.
        var clientId = Guid.NewGuid();

        // Inserta el cliente inicial.
        db.Clients.Add(new Banking.Infrastructure.Persistence.Entities.ClientRow
        {
            Id = clientId,
            FullName = "Ana López",
            DateOfBirth = new DateOnly(1995, 4, 12),
            Sex = "F",
            MonthlyIncomeMinorUnits = 2500000,
            IncomeCurrency = "NIO"
        });

        // Persiste el cliente en SQLite.
        await db.SaveChangesAsync();

        // Inserta la primera cuenta con número válido.
        db.Accounts.Add(new Banking.Infrastructure.Persistence.Entities.AccountRow
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            AccountNumber = "ACC-20261007-0048",
            Currency = "USD",
            BalanceMinorUnits = 0,
            CreatedAtUtc = new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc)
        });

        // Inserta una segunda cuenta con el mismo número para provocar violación UNIQUE.
        db.Accounts.Add(new Banking.Infrastructure.Persistence.Entities.AccountRow
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            AccountNumber = "ACC-20261007-0048",
            Currency = "USD",
            BalanceMinorUnits = 0,
            CreatedAtUtc = new DateTime(2026, 10, 7, 0, 0, 1, DateTimeKind.Utc)
        });

        // Verifica que SQLite rechace el duplicado al guardar cambios.
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DepositsAndWithdrawals_Work()
    {
        // Integra API + SQLite real para validar depósito y retiro secuencial.
        using var harness = await CreateHarnessAsync();

        // Crea una cuenta inicial para operar sobre ella.
        var accountNumber = await CreateAccountAsync(harness.Client, 0m);

        // Ejecuta un depósito por HTTP real.
        Assert.Equal(HttpStatusCode.Created, (await harness.Client.PostAsJsonAsync($"/api/accounts/{accountNumber}/deposits", new { amount = 100.00m })).StatusCode);

        // Ejecuta un retiro por HTTP real.
        Assert.Equal(HttpStatusCode.Created, (await harness.Client.PostAsJsonAsync($"/api/accounts/{accountNumber}/withdrawals", new { amount = 40.00m })).StatusCode);

        // Consulta el saldo resultante.
        var balance = await harness.Client.GetFromJsonAsync<BalanceResponseDto>($"/api/accounts/{accountNumber}/balance");

        // Verifica el saldo final esperado.
        Assert.Equal(60.0m, balance!.Balance);
    }

    [Fact]
    public async Task RollbackOnWithdrawalError()
    {
        // Integra API + SQLite real para validar rollback al fallar por fondos insuficientes.
        using var harness = await CreateHarnessAsync();

        // Crea una cuenta con saldo bajo.
        var accountNumber = await CreateAccountAsync(harness.Client, 50m);

        // Intenta retirar más de lo disponible.
        var response = await harness.Client.PostAsJsonAsync($"/api/accounts/{accountNumber}/withdrawals", new { amount = 80.00m });

        // Verifica que el caso de negocio se responda con 409.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Consulta el saldo luego del intento fallido.
        var balance = await harness.Client.GetFromJsonAsync<BalanceResponseDto>($"/api/accounts/{accountNumber}/balance");

        // Verifica que el saldo se mantuvo intacto.
        Assert.Equal(50.0m, balance!.Balance);
    }

    [Fact]
    public async Task ConcurrentWithdrawals_DoNotOverdraw()
    {
        // Integra API + SQLite real para validar que dos retiros concurrentes no sobregiren la cuenta.
        using var harness = await CreateHarnessAsync();

        // Crea una cuenta con saldo suficiente para un único retiro de 80.
        var accountNumber = await CreateAccountAsync(harness.Client, 100m);

        // Dispara dos retiros simultáneos usando clientes HTTP reales.
        var task1 = harness.Client.PostAsJsonAsync($"/api/accounts/{accountNumber}/withdrawals", new { amount = 80.00m });
        var task2 = harness.Client.PostAsJsonAsync($"/api/accounts/{accountNumber}/withdrawals", new { amount = 80.00m });
        await Task.WhenAll(task1, task2);

        // Recupera los códigos de estado resultantes.
        var statuses = new[] { (await task1).StatusCode, (await task2).StatusCode };

        // Verifica que al menos uno de los retiros haya sido confirmado.
        Assert.Contains(HttpStatusCode.Created, statuses);

        // Verifica que el otro falle por conflicto de negocio o conflicto transitorio.
        Assert.Contains(statuses, status => status is HttpStatusCode.Conflict or HttpStatusCode.ServiceUnavailable);

        // Consulta el saldo final.
        var balance = await harness.Client.GetFromJsonAsync<BalanceResponseDto>($"/api/accounts/{accountNumber}/balance");

        // Verifica que el saldo final sea consistente.
        Assert.Equal(20.0m, balance!.Balance);
    }

    [Fact]
    public async Task ConcurrentDeposits_StayConsistent()
    {
        // Integra API + SQLite real para validar depósitos concurrentes.
        using var harness = await CreateHarnessAsync();

        // Crea una cuenta inicial.
        var accountNumber = await CreateAccountAsync(harness.Client, 100m);

        // Dispara dos depósitos concurrentes.
        var task1 = harness.Client.PostAsJsonAsync($"/api/accounts/{accountNumber}/deposits", new { amount = 30.00m });
        var task2 = harness.Client.PostAsJsonAsync($"/api/accounts/{accountNumber}/deposits", new { amount = 50.00m });
        await Task.WhenAll(task1, task2);

        // Recupera ambos códigos de estado.
        var statuses = new[] { (await task1).StatusCode, (await task2).StatusCode };

        // Verifica que ambos depósitos se confirmen.
        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.Created, status));

        // Consulta el saldo final.
        var balance = await harness.Client.GetFromJsonAsync<BalanceResponseDto>($"/api/accounts/{accountNumber}/balance");

        // Verifica la suma total esperada.
        Assert.Equal(180.0m, balance!.Balance);
    }

    [Fact]
    public async Task HttpErrorContract_UsesJsonFormat()
    {
        // Integra API + SQLite real para verificar el contrato JSON de errores.
        using var harness = await CreateHarnessAsync();

        // Crea una cuenta con saldo insuficiente para probar el error.
        var accountNumber = await CreateAccountAsync(harness.Client, 10m);

        // Intenta retirar demasiado dinero.
        var response = await harness.Client.PostAsJsonAsync($"/api/accounts/{accountNumber}/withdrawals", new { amount = 80.00m });

        // Verifica el código HTTP correcto.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Lee el error en formato JSON estructurado.
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponseDto>();

        // Verifica el código de error estable.
        Assert.Equal("INSUFFICIENT_FUNDS", body!.Code);

        // Verifica que el mensaje sea legible y no vacío.
        Assert.False(string.IsNullOrWhiteSpace(body.Message));
    }

    // Crea un cliente vía API real para usarlo en otros escenarios de integración.
    private static async Task<Guid> CreateClientAsync(HttpClient client)
    {
        // Envía la solicitud HTTP al endpoint de clientes.
        var response = await client.PostAsJsonAsync("/api/clients", new
        {
            fullName = "Ana López",
            dateOfBirth = "1995-04-12",
            sex = "F",
            monthlyIncome = 25000.00m,
            incomeCurrency = "NIO"
        });

        // Verifica que la operación haya sido exitosa.
        response.EnsureSuccessStatusCode();

        // Lee la respuesta tipada.
        var body = await response.Content.ReadFromJsonAsync<ClientResponseDto>();

        // Devuelve el identificador generado.
        return body!.Id;
    }

    // Crea una cuenta vía API real con saldo inicial configurable.
    private static async Task<string> CreateAccountAsync(HttpClient client, decimal initialBalance)
    {
        // Crea primero el cliente dueño de la cuenta.
        var clientId = await CreateClientAsync(client);

        // Envía la solicitud HTTP de apertura de cuenta.
        var response = await client.PostAsJsonAsync("/api/accounts", new
        {
            clientId,
            currency = "USD",
            initialBalance
        });

        // Verifica que la cuenta se haya creado correctamente.
        response.EnsureSuccessStatusCode();

        // Lee la respuesta tipada.
        var body = await response.Content.ReadFromJsonAsync<AccountResponseDto>();

        // Devuelve el número de cuenta generado.
        return body!.AccountNumber;
    }

    // Prepara un harness de integración con SQLite real y DI controlada.
    private static async Task<ApiHarness> CreateHarnessAsync()
    {
        // Genera una base SQLite temporal por prueba.
        var dbPath = Path.Combine(Path.GetTempPath(), $"banking-{Guid.NewGuid():N}.db");

        // Configura la fábrica de WebApplicationFactory para apuntar al archivo temporal.
        var factory = new TestFactory(dbPath, services =>
        {
            // Sustituye el reloj para que las pruebas sean deterministas.
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(new IncrementingClock(new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc)));

            // Sustituye el generador para reducir la aleatoriedad en pruebas.
            services.RemoveAll<IAccountNumberGenerator>();
            services.AddSingleton<IAccountNumberGenerator>(new RandomAccountNumberGenerator(new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc)));
        });

        // Aplica las migraciones sobre SQLite real.
        await factory.MigrateAsync();

        // Devuelve el harness listo para usar.
        return new ApiHarness(dbPath, factory, factory.CreateClient());
    }

    // Empaqueta los recursos de prueba de integración.
    private sealed class ApiHarness : IDisposable
    {
        public ApiHarness(string dbPath, TestFactory factory, HttpClient client)
        {
            DbPath = dbPath;
            Factory = factory;
            Client = client;
        }

        // Ruta al archivo SQLite temporal.
        public string DbPath { get; }

        // Cliente HTTP real contra la API levantada por test host.
        public HttpClient Client { get; }

        // Fábrica ASP.NET Core usada por la prueba.
        public TestFactory Factory { get; }

        // Libera cliente y fábrica, y borra el archivo temporal.
        public void Dispose()
        {
            // Libera el cliente HTTP.
            Client.Dispose();

            // Libera la fábrica del host de pruebas.
            Factory.Dispose();

            // Elimina la base temporal si existe.
            if (File.Exists(DbPath))
            {
                File.Delete(DbPath);
            }
        }
    }

    // Fábrica personalizada para cambiar la cadena de conexión y servicios.
    private sealed class TestFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath;
        private readonly Action<IServiceCollection>? _configureServices;

        public TestFactory(string dbPath, Action<IServiceCollection>? configureServices)
        {
            _dbPath = dbPath;
            _configureServices = configureServices;
        }

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            // Sobrescribe la cadena de conexión por una base SQLite temporal.
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Banking"] = $"Data Source={_dbPath}"
                });
            });

            // Permite sustituir reloj/generador en el contenedor de DI.
            builder.ConfigureServices(services => _configureServices?.Invoke(services));
        }

        // Aplica migraciones sobre la base temporal.
        public async Task MigrateAsync()
        {
            // Crea un scope para resolver el DbContext real.
            using var scope = Services.CreateScope();

            // Obtiene el contexto real de EF Core.
            var db = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

            // Ejecuta las migraciones pendientes.
            await db.Database.MigrateAsync();
        }
    }

    // Generador de números de cuenta pseudoaleatorio para flujos normales de integración.
    private sealed class RandomAccountNumberGenerator : IAccountNumberGenerator
    {
        private readonly DateOnly _date;

        public RandomAccountNumberGenerator(DateTime baseTime) => _date = DateOnly.FromDateTime(baseTime);

        public string Generate() => $"ACC-{_date:yyyyMMdd}-{Random.Shared.Next(10000):0000}";
    }

    // Reloj fijo incrementable para timestamps deterministas en integración.
    private sealed class IncrementingClock : IClock
    {
        private long _ticks;
        private readonly DateTime _baseTime;

        public IncrementingClock(DateTime baseTime) => _baseTime = baseTime;

        public DateTime UtcNow => _baseTime.AddSeconds(Interlocked.Increment(ref _ticks));
    }

    // DTO tipado de respuesta de cliente.
    private sealed record ClientResponseDto(Guid Id, string FullName, DateOnly DateOfBirth, string Sex, decimal MonthlyIncome, string IncomeCurrency);

    // DTO tipado de respuesta de cuenta.
    private sealed record AccountResponseDto(Guid Id, Guid ClientId, string AccountNumber, string Currency, decimal Balance, DateTime CreatedAtUtc);

    // DTO tipado de respuesta de saldo.
    private sealed record BalanceResponseDto(string AccountNumber, string Currency, decimal Balance);

    // DTO tipado de respuesta de transacción.
    private sealed record TransactionResponseDto(Guid TransactionId, string AccountNumber, string Type, decimal Amount, DateTime TimestampUtc, decimal BalanceAfter);

    // DTO tipado de error de la API.
    private sealed record ApiErrorResponseDto(string Code, string Message, object? Details);
}
