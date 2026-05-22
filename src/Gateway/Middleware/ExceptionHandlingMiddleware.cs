using System.Net;
using System.Text.Json;
using FluentValidation;

namespace TaxiCompare.Gateway.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            ValidationException ve => (HttpStatusCode.BadRequest, string.Join("; ", ve.Errors.Select(e => e.ErrorMessage))),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            InvalidOperationException ioe => (HttpStatusCode.Conflict, ioe.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            _ => (HttpStatusCode.InternalServerError, "An internal error occurred")
        };

        context.Response.StatusCode = (int)statusCode;
        var response = new { error = message, statusCode = (int)statusCode };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
