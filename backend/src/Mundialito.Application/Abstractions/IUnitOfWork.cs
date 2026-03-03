namespace Mundialito.Application.Abstractions;

/// <summary>
/// Contrato del Unit of Work. Controla el commit de todos los cambios
/// realizados por los repositorios EF durante un Command.
/// Solo Infrastructure lo implementa; Application solo lo consume.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persiste todos los cambios pendientes en la base de datos.
    /// </summary>
    Task CommitAsync(CancellationToken ct = default);
}
