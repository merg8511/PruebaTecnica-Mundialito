namespace Mundialito.Application.Abstractions;

/// <summary>
/// Registro de idempotencia leído desde el read-side (Dapper).
/// </summary>
public sealed record IdempotencyRecord(
    string RequestHash,
    int    ResponseStatusCode,
    string ResponseBody);

/// <summary>
/// Servicio de lectura (Dapper) para consultar registros de idempotencia.
/// CQRS: solo lectura, nunca escribe.
/// </summary>
public interface IIdempotencyQueryService
{
    /// <summary>
    /// Busca un registro de idempotencia por su key.
    /// Devuelve null si no existe.
    /// </summary>
    Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken ct = default);
}
