using Mundialito.Application.Common;
using Mundialito.Application.DTOs.Teams;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Abstractions.QueryServices;

/// <summary>
/// Servicio de consulta de Equipos (Dapper en Infrastructure).
/// Solo Query Handlers / controladores GET deben usarlo.
/// ListAsync devuelve Result: si pageRequest/sortBy es inválido → Result.Fail(PAGINATION_INVALID).
/// </summary>
public interface ITeamsQueryService
{
    /// <summary>
    /// Devuelve un listado paginado de equipos con filtros y orden aplicados en DB.
    /// Devuelve Result.Fail(PAGINATION_INVALID) si pageRequest o sortBy son inválidos.
    /// </summary>
    Task<Result<PaginationResponse<TeamResponse>>> ListAsync(
        PageRequest pageRequest,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Devuelve un equipo por su Id, o null si no existe.
    /// </summary>
    Task<TeamResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
