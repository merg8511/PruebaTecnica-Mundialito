using Mundialito.Domain.Teams;

namespace Mundialito.Application.Abstractions.Repositories;

/// <summary>
/// Repositorio de escritura para la entidad <see cref="Team"/> (EF Core en Infrastructure).
/// Solo los Command Handlers deben usarlo.
/// </summary>
public interface ITeamRepository
{
    /// <summary>Agrega un nuevo equipo al contexto (pendiente de commit).</summary>
    void Add(Team team);

    /// <summary>Busca un equipo por su Id. Devuelve null si no existe.</summary>
    Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Busca un equipo por nombre exacto (case-insensitive). Devuelve null si no existe.</summary>
    Task<Team?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Marca la entidad como modificada en el contexto (pendiente de commit).</summary>
    void Update(Team team);

    /// <summary>Marca la entidad para eliminación en el contexto (pendiente de commit).</summary>
    void Remove(Team team);
}
