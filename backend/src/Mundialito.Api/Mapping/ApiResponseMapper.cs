using Microsoft.AspNetCore.Mvc;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Api.Mapping;

/// <summary>
/// Error envelope único :
/// { "errorCode": "...", "message": "...", "traceId": "..." }
/// </summary>
public sealed record ErrorEnvelope(string ErrorCode, string Message, string TraceId);

/// <summary>
/// Mapeador centralizado de Result/Result&lt;T&gt; a IActionResult.
/// Aplica los status codes cerrados 
///   - Success POST create → 201 Created
///   - Success GET/PUT    → 200 OK
///   - Failure           → 400/404/409/500 según errorCode + error envelope con traceId
///
/// Catálogo cerrado (CATÁLOGO FINAL DE ERRORCODES):
///   400 → VALIDATION_ERROR, PAGINATION_INVALID, IDEMPOTENCY_KEY_REQUIRED,
///          MATCH_RESULT_INCONSISTENT, PLAYER_NOT_IN_MATCH
///   404 → TEAM_NOT_FOUND, PLAYER_NOT_FOUND, MATCH_NOT_FOUND
///   409 → TEAM_NAME_CONFLICT, MATCH_ALREADY_PLAYED, TEAM_HAS_DEPENDENCIES,
///          IDEMPOTENCY_KEY_CONFLICT, RESOURCE_CONFLICT
///   500 → INTERNAL_ERROR (solo middleware); código desconocido → 500 también.
/// </summary>
public static class ApiResponseMapper
{
    // ── Catálogo cerrado 400 ───────────────────────────────────────
    private static readonly HashSet<string> Codes400 = new(StringComparer.OrdinalIgnoreCase)
    {
        DomainErrors.ValidationError,
        DomainErrors.PaginationInvalid,
        DomainErrors.IdempotencyKeyRequired,
        DomainErrors.MatchResultInconsistent,
        DomainErrors.PlayerNotInMatch
    };

    // ── Catálogo cerrado 404 ───────────────────────────────────────
    private static readonly HashSet<string> Codes404 = new(StringComparer.OrdinalIgnoreCase)
    {
        DomainErrors.TeamNotFound,
        DomainErrors.PlayerNotFound,
        DomainErrors.MatchNotFound
    };

    // ── Catálogo cerrado 409 ───────────────────────────────────────
    private static readonly HashSet<string> Codes409 = new(StringComparer.OrdinalIgnoreCase)
    {
        DomainErrors.TeamNameConflict,
        DomainErrors.MatchAlreadyPlayed,
        DomainErrors.TeamHasDependencies,
        DomainErrors.IdempotencyKeyConflict,
        DomainErrors.ResourceConflict
    };

    // ── Catálogo cerrado 500 ───────────────────────────────────────
    private static readonly HashSet<string> Codes500 = new(StringComparer.OrdinalIgnoreCase)
    {
        DomainErrors.InternalError
    };

    // ── Helpers de status ─────────────────────────────────────────────────────

    /// <summary>
    /// Mapea errorCode al HTTP status según el catálogo cerrado.
    /// Precedencia: 500 → 404 → 409 → 400 → 500 (fallback para códigos desconocidos).
    /// No existe un default silencioso a 400: un código no reconocido es un error del sistema → 500.
    /// </summary>
    private static int StatusCodeFor(string errorCode)
    {
        if (Codes500.Contains(errorCode)) return 500;
        if (Codes404.Contains(errorCode)) return 404;
        if (Codes409.Contains(errorCode)) return 409;
        if (Codes400.Contains(errorCode)) return 400;

        // Código no reconocido: no está en el catálogo cerrado → error interno del sistema.
        // Se devuelve 500 sin exponer el código desconocido en la respuesta al cliente.
        return 500;
    }

    private static string GetTraceId(HttpContext ctx) =>
        ctx.Items["TraceId"]?.ToString() ?? ctx.TraceIdentifier;

    // ── Factory de error result ───────────────────────────────────────────────

    private static IActionResult Error(HttpContext ctx, string errorCode, string? message)
    {
        var traceId = GetTraceId(ctx);
        var statusCode = StatusCodeFor(errorCode);

        // Para códigos desconocidos (500 fallback): no exponer el errorCode interno al cliente.
        var safeCode = statusCode == 500 && !Codes500.Contains(errorCode)
            ? DomainErrors.InternalError
            : errorCode;
        var safeMessage = statusCode == 500 && !Codes500.Contains(errorCode)
            ? "An unexpected error occurred."
            : (message ?? errorCode);

        var envelope = new ErrorEnvelope(safeCode, safeMessage, traceId);
        return new ObjectResult(envelope) { StatusCode = statusCode };
    }

    // ── Métodos públicos ──────────────────────────────────────────────────────

    /// <summary>
    /// Mapea Result&lt;T&gt; a 201 Created (POST create) o error.
    /// </summary>
    public static IActionResult ToCreated<T>(Result<T> result, HttpContext ctx)
    {
        if (result.IsFailure)
            return Error(ctx, result.ErrorCode!, result.ErrorMessage);
        return new ObjectResult(result.Value) { StatusCode = 201 };
    }

    /// <summary>
    /// Mapea Result&lt;T&gt; a 200 OK (GET/PUT) o error.
    /// </summary>
    public static IActionResult ToOk<T>(Result<T> result, HttpContext ctx)
    {
        if (result.IsFailure)
            return Error(ctx, result.ErrorCode!, result.ErrorMessage);
        return new OkObjectResult(result.Value);
    }

    /// <summary>
    /// Mapea Result (sin valor) a 200 OK o error.
    /// </summary>
    public static IActionResult ToOk(Result result, HttpContext ctx)
    {
        if (result.IsFailure)
            return Error(ctx, result.ErrorCode!, result.ErrorMessage);
        return new OkResult();
    }

    /// <summary>
    /// Shortcut para respuestas de listado paginado (GET list).
    /// </summary>
    public static IActionResult ToPagedOk<T>(Result<T> result, HttpContext ctx)
        where T : class => ToOk(result, ctx);

    /// <summary>
    /// Devuelve 200 con body directamente (para casos sin Result, e.g. standings).
    /// </summary>
    public static IActionResult ToOkDirect<T>(T value) =>
        new OkObjectResult(value);

    /// <summary>
    /// GET by Id: T? → 200 / 404 (con error envelope).
    /// </summary>
    public static IActionResult ToOkOrNotFound<T>(T? value, string notFoundCode, string notFoundMessage, HttpContext ctx)
        where T : class
    {
        if (value is null)
        {
            var traceId = GetTraceId(ctx);
            var envelope = new ErrorEnvelope(notFoundCode, notFoundMessage, traceId);
            return new ObjectResult(envelope) { StatusCode = 404 };
        }
        return new OkObjectResult(value);
    }

    /// <summary>
    /// DELETE: siempre 204 sin cuerpo, independientemente del resultado.
    /// "DELETE siempre 204 (exista o no exista el recurso)".
    /// </summary>
    public static IActionResult ToNoContent() => new NoContentResult();
}
