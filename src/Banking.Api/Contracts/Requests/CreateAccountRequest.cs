using System.ComponentModel.DataAnnotations;

namespace Banking.Api.Contracts.Requests;

public sealed class CreateAccountRequest
{
    public Guid ClientId { get; set; }

    [Required, RegularExpression("^(USD|NIO)$")]
    public string Currency { get; set; } = string.Empty;

    public decimal InitialBalance { get; set; }
}
