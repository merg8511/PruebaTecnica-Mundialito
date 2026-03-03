using Mundialito.Application.Common;
using Mundialito.Application.DTOs.Matches;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Abstractions.QueryServices;

/// <summary>
/// Servicio de consulta de Partidos (Dapper en Infrastructure).
/// Solo Query Handlers / controladores GET deben usarlo.
/// ListAsync devuelve Result: si pageRequest, sortBy o status son inválidos → Result.Fail(PAGINATION_INVALID).
/// status whitelist: "Scheduled" | "Played" (case-insensitive).
/// </summary>
public interface IMatchesQueryService
{
    /// <summary>
    /// Devuelve un listado paginado de partidos con filtros y orden aplicados en DB.
    /// Devuelve Result.Fail(PAGINATION_INVALID) si pageRequest, sortBy o status son inválidos.
    /// No lanza excepciones para validación normal de inputs.
    /// </summary>
    Task<Result<PaginationResponse<MatchListItemResponse>>> ListAsync(
        PageRequest pageRequest,
        DateTime? dateFrom,
        DateTime? dateTo,
        Guid? teamId,
        string? status,
        CancellationToken ct = default);

    /// <summary>
    /// Devuelve el detalle de un partido por su Id, o null si no existe.
    /// </summary>
    Task<MatchDetailResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
