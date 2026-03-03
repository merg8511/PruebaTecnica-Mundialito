using Mundialito.Domain.Results;

namespace Mundialito.Application.Abstractions.Repositories;

/// <summary>
/// Repositorio de escritura para <see cref="MatchResult"/> y <see cref="MatchGoal"/>
/// (EF Core en Infrastructure). Solo el Command Handler de RecordMatchResult lo usa.
/// </summary>
public interface IMatchResultRepository
{
    /// <summary>Agrega el resultado del partido al contexto (pendiente de commit).</summary>
    void AddResult(MatchResult result);

    /// <summary>Agrega un registro de gol de jugador al contexto (pendiente de commit).</summary>
    void AddGoal(MatchGoal goal);
}
