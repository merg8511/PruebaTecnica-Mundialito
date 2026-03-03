using Mundialito.Domain.Matches;

namespace Mundialito.Application.Abstractions.Repositories;

/// <summary>
/// Repositorio de escritura para la entidad <see cref="Match"/> (EF Core en Infrastructure).
/// Solo los Command Handlers deben usarlo.
/// </summary>
public interface IMatchRepository
{
    /// <summary>Agrega un nuevo partido al contexto (pendiente de commit).</summary>
    void Add(Match match);

    /// <summary>Busca un partido por su Id. Devuelve null si no existe.</summary>
    Task<Match?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Marca la entidad como modificada en el contexto (pendiente de commit).</summary>
    void Update(Match match);
}
