using Banking.Application.Exceptions;
using Banking.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, error) = MapException(ex);
            if (statusCode >= 500)
            {
                _logger.LogError(ex, "Unhandled error: {Code}", error.Code);
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(error);
        }
    }

    private static (int StatusCode, ApiErrorResponse Error) MapException(Exception ex)
    {
        return ex switch
        {
            NotFoundException nf => (StatusCodes.Status404NotFound, new ApiErrorResponse(nf.Code, nf.Message, null)),
            InsufficientFundsException ife => (StatusCodes.Status409Conflict, new ApiErrorResponse(ife.Code, ife.Message, null)),
            DuplicateAccountNumberException dup => (StatusCodes.Status503ServiceUnavailable, new ApiErrorResponse(dup.Code, dup.Message, null)),
            TransientStorageException tse => (StatusCodes.Status503ServiceUnavailable, new ApiErrorResponse(tse.Code, tse.Message, null)),
            ApplicationExceptionBase appEx => (StatusCodes.Status409Conflict, new ApiErrorResponse(appEx.Code, appEx.Message, null)),
            DomainException domainEx => (StatusCodes.Status400BadRequest, new ApiErrorResponse(domainEx.Code, domainEx.Message, null)),
            _ => (StatusCodes.Status500InternalServerError, new ApiErrorResponse("INTERNAL_ERROR", "Se produjo un error inesperado.", null))
        };
    }
}
