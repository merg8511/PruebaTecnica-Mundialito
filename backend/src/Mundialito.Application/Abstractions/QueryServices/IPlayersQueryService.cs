using Mundialito.Application.Common;
using Mundialito.Application.DTOs.Players;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Abstractions.QueryServices;

/// <summary>
/// Servicio de consulta de Jugadores (Dapper en Infrastructure).
/// Solo Query Handlers / controladores GET deben usarlo.
/// ListAsync devuelve Result: si pageRequest/sortBy es inválido → Result.Fail(PAGINATION_INVALID).
/// </summary>
public interface IPlayersQueryService
{
    /// <summary>
    /// Devuelve un listado paginado de jugadores con filtros y orden aplicados en DB.
    /// Devuelve Result.Fail(PAGINATION_INVALID) si pageRequest o sortBy son inválidos.
    /// </summary>
    Task<Result<PaginationResponse<PlayerResponse>>> ListAsync(
        PageRequest pageRequest,
        Guid? teamId,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Devuelve un jugador por su Id, o null si no existe.
    /// </summary>
    Task<PlayerResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
