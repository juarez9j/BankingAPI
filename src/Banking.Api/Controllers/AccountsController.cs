using Banking.Api.Contracts.Requests;
using Banking.Api.Contracts.Responses;
using Banking.Application.Models;
using Banking.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly CreateAccountService _createAccountService;
    private readonly GetBalanceService _getBalanceService;
    private readonly DepositService _depositService;
    private readonly WithdrawService _withdrawService;
    private readonly GetTransactionsService _getTransactionsService;

    public AccountsController(
        CreateAccountService createAccountService,
        GetBalanceService getBalanceService,
        DepositService depositService,
        WithdrawService withdrawService,
        GetTransactionsService getTransactionsService)
    {
        _createAccountService = createAccountService;
        _getBalanceService = getBalanceService;
        _depositService = depositService;
        _withdrawService = withdrawService;
        _getTransactionsService = getTransactionsService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _createAccountService.ExecuteAsync(new CreateAccountCommand(request.ClientId, request.Currency, request.InitialBalance), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new AccountResponse(result.Id, result.ClientId, result.AccountNumber, result.Currency, result.Balance, result.CreatedAtUtc));
    }

    [HttpGet("{accountNumber}/balance")]
    [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceResponse>> GetBalance([FromRoute] string accountNumber, CancellationToken cancellationToken)
    {
        var result = await _getBalanceService.ExecuteAsync(accountNumber, cancellationToken);
        return Ok(new BalanceResponse(result.ClientId, result.AccountNumber, result.Currency, result.Balance));
    }

    [HttpPost("{accountNumber}/deposits")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Deposit([FromRoute] string accountNumber, [FromBody] MoneyRequest request, CancellationToken cancellationToken)
    {
        var result = await _depositService.ExecuteAsync(accountNumber, new MoneyCommand(request.Amount), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new TransactionResponse(result.TransactionId, result.AccountNumber, result.Type, result.Amount, result.TimestampUtc, result.BalanceAfter));
    }

    [HttpPost("{accountNumber}/withdrawals")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Withdraw([FromRoute] string accountNumber, [FromBody] MoneyRequest request, CancellationToken cancellationToken)
    {
        var result = await _withdrawService.ExecuteAsync(accountNumber, new MoneyCommand(request.Amount), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new TransactionResponse(result.TransactionId, result.AccountNumber, result.Type, result.Amount, result.TimestampUtc, result.BalanceAfter));
    }

    [HttpGet("{accountNumber}/transactions")]
    [ProducesResponseType(typeof(IEnumerable<TransactionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetTransactions([FromRoute] string accountNumber, CancellationToken cancellationToken)
    {
        var result = await _getTransactionsService.ExecuteAsync(accountNumber, cancellationToken);
        return Ok(result.Select(x => new TransactionResponse(x.TransactionId, x.AccountNumber, x.Type, x.Amount, x.TimestampUtc, x.BalanceAfter)));
    }
}
