using Banking.Api.Middleware;
using Banking.Application.Models;
using Banking.Application.Services;
using Banking.Infrastructure;
using Banking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var details = context.ModelState
                .Where(x => x.Value is not null && x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                .ToArray();

            return new BadRequestObjectResult(new ApiErrorResponse(
                "INVALID_DATA",
                "Datos inválidos.",
                details.Length == 0 ? null : details));
        };
    });

builder.Services.AddOpenApi();
builder.Services.AddBankingInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateClientService>();
builder.Services.AddScoped<CreateAccountService>();
builder.Services.AddScoped<GetBalanceService>();
builder.Services.AddScoped<DepositService>();
builder.Services.AddScoped<WithdrawService>();
builder.Services.AddScoped<GetTransactionsService>();

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();

app.MapControllers();
app.MapOpenApi();

app.Run();

public partial class Program;
