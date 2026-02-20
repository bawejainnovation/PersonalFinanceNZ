using System.Net;
using System.Text.Json;

namespace FinancialInsights.Api.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ArgumentException exception)
        {
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, exception.Message, exception);
        }
        catch (Exception exception)
        {
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "Unexpected server error.", exception);
        }
    }

    private async Task WriteErrorAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string message,
        Exception exception)
    {
        logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            error = message,
            statusCode = context.Response.StatusCode,
            traceId = context.TraceIdentifier
        }));
    }
}
