namespace Banking.Api.Middleware;

public sealed record ApiErrorResponse(string Code, string Message, object? Details);
