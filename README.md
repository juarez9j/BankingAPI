# Banking MVP API

API REST Banking para Prueba tecnica BackEnd para clientes, cuentas y movimientos bancarios.

## Requisitos

- .NET 10 SDK
- SQLite (incluido por el proveedor EF Core)

## Estructura

```text
BankingMvp/
├── src/
│   ├── Banking.Api/
│   ├── Banking.Application/
│   ├── Banking.Domain/
│   └── Banking.Infrastructure/
├── tests/
│   ├── Banking.UnitTests/
│   └── Banking.IntegrationTests/
└── README.md
```

## Restaurar paquetes

```bash
dotnet restore BankingMvp.slnx --ignore-failed-sources
```

## Compilar

```bash
dotnet build BankingMvp.slnx -c Debug --no-restore
```

## Aplicar migraciones

La base usa EF Core + SQLite y requiere migraciones explícitas.

Si no tienes `dotnet-ef` instalado:

```bash
dotnet tool install --global dotnet-ef
```

Aplicar la migración:

```bash
dotnet ef database update \
  --project src/Banking.Infrastructure \
  --startup-project src/Banking.Api
```

Por defecto la conexión usa `Data Source=banking.db`.

## Ejecutar la API

```bash
dotnet run --project src/Banking.Api
```

## OpenAPI JSON

Disponible en:

```text
/openapi/v1.json
```

### Cómo usar OpenAPI JSON en este proyecto

1. Ejecuta la API.
2. Abre en el navegador:

```text
http://localhost:5000/openapi/v1.json
```

3. Usa ese JSON para:
   - importarlo en Swagger Editor;
   - generar clientes HTTP;
   - documentar integraciones;
   - validar cambios de contrato antes de consumir la API.

El proyecto no incluye interfaz visual Swagger, solo el JSON OpenAPI.

## Pruebas unitarias

```bash
dotnet test tests/Banking.UnitTests/Banking.UnitTests.csproj -c Debug --no-restore
```

## Pruebas de integración

```bash
dotnet test tests/Banking.IntegrationTests/Banking.IntegrationTests.csproj -c Debug --no-restore
```

## Endpoints

### 1) Crear cliente

```bash
curl -X POST http://localhost:5000/api/clients \
  -H "Content-Type: application/json" \
  -d '{
    "fullName":"Ana López",
    "dateOfBirth":"1995-04-12",
    "sex":"F",
    "monthlyIncome":25000.00,
    "incomeCurrency":"NIO"
  }'
```

### 2) Crear cuenta

```bash
curl -X POST http://localhost:5000/api/accounts \
  -H "Content-Type: application/json" \
  -d '{
    "clientId":"00000000-0000-0000-0000-000000000000",
    "currency":"USD",
    "initialBalance":150.00
  }'
```

### 3) Consultar saldo

```bash
curl http://localhost:5000/api/accounts/ACC-20261007-0048/balance
```

Respuesta esperada:

```json
{
  "clientId": "b1e61f1c-4b4a-46e1-9bdb-bf1c2e41d3f0",
  "accountNumber": "ACC-20261007-0048",
  "currency": "USD",
  "balance": 150.00
}
```

### 4) Depositar

```bash
curl -X POST http://localhost:5000/api/accounts/ACC-20261007-0048/deposits \
  -H "Content-Type: application/json" \
  -d '{"amount":100.00}'
```

### 5) Retirar

```bash
curl -X POST http://localhost:5000/api/accounts/ACC-20261007-0048/withdrawals \
  -H "Content-Type: application/json" \
  -d '{"amount":50.00}'
```

### 6) Historial

```bash
curl http://localhost:5000/api/accounts/ACC-20261007-0048/transactions
```

## Respuestas HTTP de ejemplo

### Éxito

```json
{
  "clientId": "...",
  "accountNumber": "ACC-20261007-0048",
  "currency": "USD",
  "balance": 150.00
}
```

### Error

```json
{
  "code": "INSUFFICIENT_FUNDS",
  "message": "La cuenta no dispone de fondos suficientes.",
  "details": null
}
```

## Limitaciones del MVP

- Sin autenticación ni autorización
- Sin frontend
- Sin transferencias
- Sin comisiones, intereses o tarjetas
- Sin paginación de historial
- Sin despliegue empresarial
- Pensado para una sola instancia local

## Decisiones técnicas y reglas de negocio

### Generales

- Solo existen 6 endpoints comerciales.
- Un cliente puede tener múltiples cuentas.
- La moneda del cliente (`incomeCurrency`) es independiente de la moneda de la cuenta.
- `Sex` acepta `M/F/m/f` y se normaliza a mayúsculas.
- `IncomeCurrency` y `Currency` solo aceptan `USD` o `NIO`.
- El saldo nunca puede ser negativo.
- Los movimientos son inmutables.
- El historial no tiene paginación en el MVP.
- Los retiros con fondos insuficientes devuelven `409 Conflict`.
- Los conflictos transitorios devuelven `503 Service Unavailable`.

### Almacenamiento de montos

- En C# y contratos HTTP: `decimal`.
- En SQLite: `long` en **minor units**.

Ejemplo:

- `150.25` → `15025`

Esto evita errores de redondeo binario y mantiene precisión exacta.

### Notas de uso

- El API no debe exponerse públicamente.
- La base SQLite está pensada para una sola instancia local.
- Las migraciones deben ejecutarse explícitamente.
