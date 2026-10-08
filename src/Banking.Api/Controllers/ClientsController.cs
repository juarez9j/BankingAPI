using Banking.Api.Contracts.Requests;
using Banking.Api.Contracts.Responses;
using Banking.Application.Models;
using Banking.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/clients")]
public sealed class ClientsController : ControllerBase
{
    private readonly CreateClientService _service;

    public ClientsController(CreateClientService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ExecuteAsync(new CreateClientCommand(request.FullName, request.DateOfBirth, request.Sex, request.MonthlyIncome, request.IncomeCurrency), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new ClientResponse(result.Id, result.FullName, result.DateOfBirth, result.Sex, result.MonthlyIncome, result.IncomeCurrency));
    }
}
