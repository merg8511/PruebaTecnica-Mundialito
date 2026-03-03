namespace Mundialito.Application.Abstractions;

/// <summary>
/// Unit of Work dedicado exclusivamente a IdempotencyKeys.
/// Separado del IUnitOfWork de dominio para no mezclar contextos de commit.
/// Solo Infrastructure lo implementa; el filtro lo consume a través de esta interfaz.
/// </summary>
public interface IIdempotencyUnitOfWork
{
    /// <summary>
    /// Persiste los cambios pendientes de IdempotencyKeys en la base de datos.
    /// Es el único lugar donde se llama SaveChangesAsync para esta entidad.
    /// </summary>
    Task CommitAsync(CancellationToken ct = default);
}
