using System.Diagnostics;

namespace Mundialito.Api.Middlewares;

/// <summary>
/// Middleware de trazabilidad: genera/propaga traceId y correlationId,
/// mide duración del request (elapsedMs) y emite log estructurado al finalizar.
///
/// traceId:       Activity.Current?.Id ?? HttpContext.TraceIdentifier
/// correlationId: Header X-Correlation-Id si existe; si no, Guid nuevo.
/// </summary>
public sealed class ObservabilityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ObservabilityMiddleware> _logger;

    public ObservabilityMiddleware(RequestDelegate next, ILogger<ObservabilityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        // ── TraceId ───────────────────────────────────────────────────────────
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        // ── CorrelationId ─────────────────────────────────────────────────────
        var correlationHeader = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        var correlationId = string.IsNullOrWhiteSpace(correlationHeader)
            ? Guid.NewGuid().ToString()
            : correlationHeader;

        // Exponer traceId y correlationId para que otros componentes (ej. mappers) puedan leerlo.
        context.Items["TraceId"] = traceId;
        context.Items["CorrelationId"] = correlationId;

        // ── Log scope estructurado ────────────────────────────────────────────
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["CorrelationId"] = correlationId
        });

        await _next(context);

        sw.Stop();

        // ── Log al finalizar el request ───────────────────────────────────────
        _logger.LogInformation(
            "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs}ms | traceId={TraceId} correlationId={CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds,
            traceId,
            correlationId);
    }
}
