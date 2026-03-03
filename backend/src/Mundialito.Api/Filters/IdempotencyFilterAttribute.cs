using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Mundialito.Api.Mapping;
using Mundialito.Application.Abstractions;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Api.Filters;

/// <summary>
/// Action Filter que implementa idempotencia en los 4 endpoints POST:
///   POST /teams
///   POST /teams/{teamId}/players
///   POST /matches
///   POST /matches/{id}/results
///
/// Flujo CORREGIDO (persist-before-respond):
///   1) Valida que exista el header Idempotency-Key → si falta → 400 IDEMPOTENCY_KEY_REQUIRED
///   2) Calcula RequestHash (SHA-256 hex del body exacto en bytes UTF-8)
///   3) Lookup Dapper → si existe y hash coincide → replay; si difiere → 409
///   4) Ejecuta el endpoint CAPTURANDO el response en un MemoryStream (NO escribe al cliente todavía)
///   5) Persiste con EF + UoW dedicado
///      • OK         → escribe la respuesta capturada al cliente
///      • Race/unique → re-lookup Dapper:
///            hash coincide → replay exacto del record almacenado
///            hash difiere  → 409 IDEMPOTENCY_KEY_CONFLICT
///            no existe     → 500 INTERNAL_ERROR (seguro, sin exponer detalles internos)
///   El cliente NUNCA recibe respuesta antes de que se haya resuelto de forma determinística.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class IdempotencyFilterAttribute : Attribute, IAsyncActionFilter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var ct          = httpContext.RequestAborted;

        // ─────────────────────────────────────────────────────────────────────
        // Paso 1: Verificar header Idempotency-Key
        // ─────────────────────────────────────────────────────────────────────
        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            context.Result = BuildErrorResult(httpContext,
                DomainErrors.IdempotencyKeyRequired,
                "The 'Idempotency-Key' header is required for this endpoint.",
                400);
            return;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Paso 2: Leer body exacto → RequestHash (SHA-256 hex)
        // EnableBuffering garantiza que el stream pueda resetearse para el controller.
        // ─────────────────────────────────────────────────────────────────────
        httpContext.Request.EnableBuffering();
        var bodyBytes   = await ReadBodyBytesAsync(httpContext.Request, ct);
        var requestHash = ComputeSha256Hex(bodyBytes);
        httpContext.Request.Body.Position = 0;  // Reset: el controller leerá el body fresco

        // ─────────────────────────────────────────────────────────────────────
        // Paso 3: Lookup Dapper (read side — CERO EF)
        // ─────────────────────────────────────────────────────────────────────
        var queryService = httpContext.RequestServices.GetRequiredService<IIdempotencyQueryService>();
        var existing     = await queryService.GetByKeyAsync(idempotencyKey, ct);

        if (existing is not null)
        {
            if (existing.RequestHash == requestHash)
            {
                // ── REPLAY EXACTO (hit de caché idempotente) ─────────────────
                await WriteResponseAsync(httpContext, existing.ResponseStatusCode, existing.ResponseBody, ct);
                return;
            }

            // ── CONFLICT: misma key, distinto payload ─────────────────────────
            context.Result = BuildErrorResult(httpContext,
                DomainErrors.IdempotencyKeyConflict,
                "An idempotency key conflict was detected: the same key was used with a different request payload.",
                409);
            return;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Paso 4: Capturar el response SIN enviarlo al cliente todavía.
        // Sustituimos Response.Body por un MemoryStream propio; el framework MVC
        // escribe el JSON del controller AHÍ. Solo restauramos el stream original
        // en el finally; la copia al cliente ocurre después de persistir.
        // ─────────────────────────────────────────────────────────────────────
        var originalBodyStream = httpContext.Response.Body;
        var captureStream      = new MemoryStream();
        httpContext.Response.Body = captureStream;

        ActionExecutedContext executed;
        try
        {
            executed = await next();
        }
        catch
        {
            // Excepción no manejada: restaurar el stream original y propagar
            // para que ExceptionHandlingMiddleware devuelva 500.
            httpContext.Response.Body = originalBodyStream;
            captureStream.Dispose();
            throw;
        }
        finally
        {
            // SOLO restauramos el puntero; NO copiamos el captureStream todavía.
            // La decisión de qué responder se toma después de persistir.
            httpContext.Response.Body = originalBodyStream;
        }

        // Si el action propagó una excepción manejada internamente, abortamos.
        // ExceptionHandlingMiddleware habrá capturado o capturará el error.
        if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            captureStream.Dispose();
            return;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Paso 5: Leer status y body capturados (todavía NO en el cliente)
        // ─────────────────────────────────────────────────────────────────────
        var capturedStatusCode = httpContext.Response.StatusCode;
        captureStream.Position = 0;
        var capturedBody       = await new StreamReader(captureStream, Encoding.UTF8, leaveOpen: true)
                                        .ReadToEndAsync(ct);
        captureStream.Dispose();

        // ─────────────────────────────────────────────────────────────────────
        // Paso 6: Persistir con EF + UoW dedicado (ANTES de responder al cliente)
        // ─────────────────────────────────────────────────────────────────────
        var repository = httpContext.RequestServices.GetRequiredService<IIdempotencyRepository>();
        var uow        = httpContext.RequestServices.GetRequiredService<IIdempotencyUnitOfWork>();

        try
        {
            await repository.SaveAsync(idempotencyKey, requestHash, capturedStatusCode, capturedBody, ct);
            await uow.CommitAsync(ct);

            // ── ÉXITO: persistido → ahora enviamos la respuesta real al cliente ──
            await WriteResponseAsync(httpContext, capturedStatusCode, capturedBody, ct);
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            // ─────────────────────────────────────────────────────────────────
            // Paso 7: Race condition — otro request ganó el INSERT concurrente.
            // Re-lookup con Dapper para determinar si es replay o conflicto.
            // El cliente AÚN no recibió respuesta (corrección del bug original).
            // ─────────────────────────────────────────────────────────────────
            var raceRecord = await queryService.GetByKeyAsync(idempotencyKey, ct);

            if (raceRecord is not null)
            {
                if (raceRecord.RequestHash == requestHash)
                {
                    // Mismo payload ganado en la race → replay exacto del record almacenado
                    await WriteResponseAsync(httpContext, raceRecord.ResponseStatusCode, raceRecord.ResponseBody, ct);
                }
                else
                {
                    // Payload distinto ganó la race → 409 conflict determinístico
                    await WriteErrorResponseAsync(httpContext,
                        DomainErrors.IdempotencyKeyConflict,
                        "An idempotency key conflict was detected: the same key was used with a different request payload.",
                        409, ct);
                }
            }
            else
            {
                // Caso extremadamente raro: constraint falló pero el re-lookup no encontró el registro.
                // Responder 500 de forma segura, sin exponer internals.
                await WriteErrorResponseAsync(httpContext,
                    DomainErrors.InternalError,
                    "An unexpected error occurred while processing the idempotency key.",
                    500, ct);
            }
        }
    }

    // ── Helpers privados ───────────────────────────────────────────────────────

    private static async Task<byte[]> ReadBodyBytesAsync(HttpRequest request, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    private static string ComputeSha256Hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetTraceId(HttpContext ctx) =>
        ctx.Items["TraceId"]?.ToString() ?? ctx.TraceIdentifier;

    /// <summary>
    /// Escribe la respuesta final al cliente (response real).
    /// Usado tanto para el happy-path como para el replay exacto.
    /// </summary>
    private static async Task WriteResponseAsync(
        HttpContext ctx,
        int statusCode,
        string body,
        CancellationToken ct)
    {
        ctx.Response.StatusCode = statusCode;

        if (!string.IsNullOrEmpty(body))
        {
            ctx.Response.ContentType = "application/json; charset=utf-8";
            var bytes = Encoding.UTF8.GetBytes(body);
            await ctx.Response.Body.WriteAsync(bytes, ct);
        }
    }

    /// <summary>
    /// Escribe una respuesta de error con el envelope estándar directamente al stream.
    /// Se usa en el manejo de race conditions, cuando context.Result ya no aplica
    /// (el pipeline MVC ya terminó y la respuesta se escribe directamente al Body).
    /// </summary>
    private static async Task WriteErrorResponseAsync(
        HttpContext ctx,
        string errorCode,
        string message,
        int statusCode,
        CancellationToken ct)
    {
        var traceId  = GetTraceId(ctx);
        var envelope = new ErrorEnvelope(errorCode, message, traceId);
        var json     = JsonSerializer.Serialize(envelope, JsonOptions);

        ctx.Response.StatusCode  = statusCode;
        ctx.Response.ContentType = "application/json; charset=utf-8";

        var bytes = Encoding.UTF8.GetBytes(json);
        await ctx.Response.Body.WriteAsync(bytes, ct);
    }

    /// <summary>
    /// Construye un IActionResult de error con el envelope estándar.
    /// Se usa en la etapa PRE-ejecución del pipeline MVC (pasos 1 y 3),
    /// donde context.Result aún puede interrumpir el pipeline limpiamente.
    /// </summary>
    private static IActionResult BuildErrorResult(
        HttpContext ctx,
        string errorCode,
        string message,
        int statusCode)
    {
        var envelope = new ErrorEnvelope(errorCode, message, GetTraceId(ctx));
        return new ObjectResult(envelope) { StatusCode = statusCode };
    }

    /// <summary>
    /// Detecta violación de unique constraint en SQL Server (error 2627 / 2601),
    /// incluyendo cuando EF Core la envuelve en DbUpdateException.
    /// Se usa para manejar race conditions sin exponer excepción al cliente.
    /// </summary>
    private static bool IsUniqueConstraintViolation(Exception ex)
    {
        var current = ex;
        while (current is not null)
        {
            if (current is SqlException sqlEx &&
                (sqlEx.Number == 2627 || sqlEx.Number == 2601))
                return true;

            current = current.InnerException;
        }
        return false;
    }
}
