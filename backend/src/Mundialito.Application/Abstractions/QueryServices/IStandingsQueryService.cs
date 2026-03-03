using Mundialito.Application.DTOs.Standings;

namespace Mundialito.Application.Abstractions.QueryServices;

/// <summary>
/// Servicio de consulta de Clasificación (Dapper en Infrastructure).
/// Solo Query Handlers / controladores GET deben usarlo.
/// </summary>
public interface IStandingsQueryService
{
    /// <summary>
    /// Devuelve la clasificación completa del torneo, ordenada obligatoriamente por:
    /// points desc → goalDifference desc → goalsFor desc.
    /// </summary>
    Task<IReadOnlyList<StandingItemResponse>> ListAsync(CancellationToken ct = default);
}
