using System.Net;
using System.Text.Json;
using Mundialito.Api.Mapping;

namespace Mundialito.Api.Middlewares;

/// <summary>
/// Middleware de manejo global de excepciones no controladas.
/// Captura cualquier excepción, loggea con traceId/correlationId y
/// responde 500 con el error envelope
///   { "errorCode": "INTERNAL_ERROR", "message": "...", "traceId": "..." }
/// </summary>
public sealed class ExceptionHandlingMiddleware
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
            var traceId = context.Items["TraceId"]?.ToString()
                ?? context.TraceIdentifier;
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "-";

            _logger.LogError(
                ex,
                "Unhandled exception | traceId={TraceId} correlationId={CorrelationId}",
                traceId,
                correlationId);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var envelope = new ErrorEnvelope(
                "INTERNAL_ERROR",
                "An unexpected error occurred.",
                traceId);

            var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
