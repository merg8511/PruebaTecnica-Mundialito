using Mundialito.Domain.Players;

namespace Mundialito.Application.Abstractions.Repositories;

/// <summary>
/// Repositorio de escritura para la entidad <see cref="Player"/> (EF Core en Infrastructure).
/// Solo los Command Handlers deben usarlo.
/// </summary>
public interface IPlayerRepository
{
    /// <summary>Agrega un nuevo jugador al contexto (pendiente de commit).</summary>
    void Add(Player player);

    /// <summary>Busca un jugador por su Id. Devuelve null si no existe.</summary>
    Task<Player?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Marca la entidad como modificada en el contexto (pendiente de commit).</summary>
    void Update(Player player);

    /// <summary>Marca la entidad para eliminación en el contexto (pendiente de commit).</summary>
    void Remove(Player player);
}
