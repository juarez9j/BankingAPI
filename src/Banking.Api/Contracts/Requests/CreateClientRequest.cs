using System.ComponentModel.DataAnnotations;

namespace Banking.Api.Contracts.Requests;

public sealed class CreateClientRequest
{
    [Required, MinLength(1)]
    public string FullName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    [Required, RegularExpression("^[MmFf]$")]
    public string Sex { get; set; } = string.Empty;

    public decimal MonthlyIncome { get; set; }

    [Required, RegularExpression("^(USD|NIO)$")]
    public string IncomeCurrency { get; set; } = string.Empty;
}
