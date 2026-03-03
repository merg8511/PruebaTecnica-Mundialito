using Mundialito.Application.Common;
using Mundialito.Application.DTOs.Scorers;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Abstractions.QueryServices;

/// <summary>
/// Servicio de consulta de Goleadores (Dapper en Infrastructure).
/// Solo Query Handlers / controladores GET deben usarlo.
/// ListAsync devuelve Result: si pageRequest/sortBy es inválido → Result.Fail(PAGINATION_INVALID).
/// </summary>
public interface IScorersQueryService
{
    /// <summary>
    /// Devuelve un listado paginado de goleadores con filtros y orden aplicados en DB.
    /// Devuelve Result.Fail(PAGINATION_INVALID) si pageRequest o sortBy son inválidos.
    /// </summary>
    Task<Result<PaginationResponse<ScorerItemResponse>>> ListAsync(
        PageRequest pageRequest,
        Guid? teamId,
        string? search,
        CancellationToken ct = default);
}
