using Microsoft.AspNetCore.Mvc;
using Mundialito.Api.Filters;
using Mundialito.Api.Mapping;
using Mundialito.Application.Abstractions.QueryServices;
using Mundialito.Application.Common;
using Mundialito.Application.DTOs.Teams;
using Mundialito.Application.Features.Teams;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Api.Controllers;

/// <summary>
/// Endpoint: /teams
/// POST /teams           → CreateTeamUseCase         → 201 / 400 / 409
/// GET  /teams           → ITeamsQueryService.List   → 200 / 400
/// GET  /teams/{id}      → ITeamsQueryService.GetById → 200 / 404
/// PUT  /teams/{id}      → UpdateTeamUseCase          → 200 / 400 / 404 / 409
/// DELETE /teams/{id}    → DeleteTeamUseCase          → 204 siempre
/// </summary>
[ApiController]
[Route("teams")]
public sealed class TeamsController : ControllerBase
{
    private readonly CreateTeamUseCase _create;
    private readonly UpdateTeamUseCase _update;
    private readonly DeleteTeamUseCase _delete;
    private readonly ITeamsQueryService _query;

    public TeamsController(
        CreateTeamUseCase create,
        UpdateTeamUseCase update,
        DeleteTeamUseCase delete,
        ITeamsQueryService query)
    {
        _create = create;
        _update = update;
        _delete = delete;
        _query = query;
    }

    // ── POST /teams ───────────────────────────────────────────────────────────
    [HttpPost]
    [IdempotencyFilter]
    public async Task<IActionResult> CreateTeam(
        [FromBody] CreateTeamRequest request,
        CancellationToken ct)
    {
        var result = await _create.ExecuteAsync(request, ct);
        return ApiResponseMapper.ToCreated(result, HttpContext);
    }

    // ── GET /teams ────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ListTeams(
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

        var result = await _query.ListAsync(pageRequest, search, ct);
        return ApiResponseMapper.ToPagedOk(result, HttpContext);
    }

    // ── GET /teams/{id} ───────────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTeam(Guid id, CancellationToken ct)
    {
        var team = await _query.GetByIdAsync(id, ct);
        return ApiResponseMapper.ToOkOrNotFound(
            team,
            DomainErrors.TeamNotFound,
            $"Team '{id}' was not found.",
            HttpContext);
    }

    // ── PUT /teams/{id} ───────────────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTeam(
        Guid id,
        [FromBody] UpdateTeamRequest request,
        CancellationToken ct)
    {
        var result = await _update.ExecuteAsync(id, request, ct);
        return ApiResponseMapper.ToOk(result, HttpContext);
    }

    // ── DELETE /teams/{id} ────────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTeam(Guid id, CancellationToken ct)
    {
        await _delete.ExecuteAsync(id, ct);
        return ApiResponseMapper.ToNoContent();   // siempre 204
    }
}
