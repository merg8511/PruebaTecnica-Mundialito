using Microsoft.AspNetCore.Mvc;
using Mundialito.Api.Filters;
using Mundialito.Api.Mapping;
using Mundialito.Application.Abstractions.QueryServices;
using Mundialito.Application.Common;
using Mundialito.Application.DTOs.Players;
using Mundialito.Application.Features.Players;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Api.Controllers;

/// <summary>
/// Endpoints para jugadores:
/// POST   /teams/{teamId}/players          → CreatePlayerUseCase         → 201 / 400 / 404
/// GET    /players                         → IPlayersQueryService.List   → 200 / 400
/// GET    /teams/{teamId}/players          → IPlayersQueryService.List (con teamId filter) → 200 / 400
/// GET    /players/{id}                    → IPlayersQueryService.GetById → 200 / 404
/// PUT    /players/{id}                    → UpdatePlayerUseCase          → 200 / 400 / 404
/// DELETE /players/{id}                   → DeletePlayerUseCase          → 204 siempre
/// </summary>
[ApiController]
public sealed class PlayersController : ControllerBase
{
    private readonly CreatePlayerUseCase _create;
    private readonly UpdatePlayerUseCase _update;
    private readonly DeletePlayerUseCase _delete;
    private readonly IPlayersQueryService _query;

    public PlayersController(
        CreatePlayerUseCase create,
        UpdatePlayerUseCase update,
        DeletePlayerUseCase delete,
        IPlayersQueryService query)
    {
        _create = create;
        _update = update;
        _delete = delete;
        _query = query;
    }

    // ── POST /teams/{teamId}/players ──────────────────────────────────────────
    [HttpPost("teams/{teamId:guid}/players")]
    [IdempotencyFilter]
    public async Task<IActionResult> CreatePlayer(
        Guid teamId,
        [FromBody] CreatePlayerRequest request,
        CancellationToken ct)
    {
        var result = await _create.ExecuteAsync(teamId, request, ct);
        return ApiResponseMapper.ToCreated(result, HttpContext);
    }

    // ── GET /players ──────────────────────────────────────────────────────────
    [HttpGet("players")]
    public async Task<IActionResult> ListPlayers(
        [FromQuery] int pageNumber = PageRequest.DefaultPageNumber,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = PageRequest.DefaultSortDirection,
        [FromQuery] Guid? teamId = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var pageRequest = new PageRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _query.ListAsync(pageRequest, teamId, search, ct);
        return ApiResponseMapper.ToPagedOk(result, HttpContext);
    }

    // ── GET /teams/{teamId}/players ───────────────────────────────────────────
    [HttpGet("teams/{teamId:guid}/players")]
    public async Task<IActionResult> ListPlayersByTeam(
        Guid teamId,
        [FromQuery] int pageNumber = PageRequest.DefaultPageNumber,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = PageRequest.DefaultSortDirection,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var pageRequest = new PageRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _query.ListAsync(pageRequest, teamId, search, ct);
        return ApiResponseMapper.ToPagedOk(result, HttpContext);
    }

    // ── GET /players/{id} ─────────────────────────────────────────────────────
    [HttpGet("players/{id:guid}")]
    public async Task<IActionResult> GetPlayer(Guid id, CancellationToken ct)
    {
        var player = await _query.GetByIdAsync(id, ct);
        return ApiResponseMapper.ToOkOrNotFound(
            player,
            DomainErrors.PlayerNotFound,
            $"Player '{id}' was not found.",
            HttpContext);
    }

    // ── PUT /players/{id} ─────────────────────────────────────────────────────
    [HttpPut("players/{id:guid}")]
    public async Task<IActionResult> UpdatePlayer(
        Guid id,
        [FromBody] UpdatePlayerRequest request,
        CancellationToken ct)
    {
        var result = await _update.ExecuteAsync(id, request, ct);
        return ApiResponseMapper.ToOk(result, HttpContext);
    }

    // ── DELETE /players/{id} ──────────────────────────────────────────────────
    [HttpDelete("players/{id:guid}")]
    public async Task<IActionResult> DeletePlayer(Guid id, CancellationToken ct)
    {
        await _delete.ExecuteAsync(id, ct);
        return ApiResponseMapper.ToNoContent();   // siempre 204
    }
}
