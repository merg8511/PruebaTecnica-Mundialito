namespace Mundialito.Application.Abstractions;

/// <summary>
/// Repositorio de escritura (EF Core) para persistir registros de idempotencia.
/// CQRS: solo escritura — agrega una entidad que luego es persistida por IIdempotencyUnitOfWork.
/// La interfaz está en Application; la entidad concreta queda en Infrastructure.
/// </summary>
public interface IIdempotencyRepository
{
    /// <summary>
    /// Persiste el registro de idempotencia completo.
    /// CommitAsync lo llama el IIdempotencyUnitOfWork.
    /// </summary>
    Task SaveAsync(
        string idempotencyKey,
        string requestHash,
        int    responseStatusCode,
        string responseBody,
        CancellationToken ct = default);
}
