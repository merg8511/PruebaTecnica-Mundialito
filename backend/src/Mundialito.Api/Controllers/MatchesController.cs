using Microsoft.AspNetCore.Mvc;
using Mundialito.Api.Filters;
using Mundialito.Api.Mapping;
using Mundialito.Application.Abstractions.QueryServices;
using Mundialito.Application.Common;
using Mundialito.Application.DTOs.Matches;
using Mundialito.Application.DTOs.Results;
using Mundialito.Application.Features.Matches;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Api.Controllers;

/// <summary>
/// Endpoints de partidos:
/// POST /matches          → CreateMatchUseCase           → 201 / 400 / 404
/// GET  /matches          → IMatchesQueryService.List    → 200 / 400
/// GET  /matches/{id}     → IMatchesQueryService.GetById → 200 / 404
///
/// Endpoint de resultados:
/// POST /matches/{id}/results → RecordMatchResultUseCase → 200 / 400 / 404 / 409
/// </summary>
[ApiController]
[Route("matches")]
public sealed class MatchesController : ControllerBase
{
    private readonly CreateMatchUseCase _createMatch;
    private readonly RecordMatchResultUseCase _recordResult;
    private readonly IMatchesQueryService _query;

    public MatchesController(
        CreateMatchUseCase createMatch,
        RecordMatchResultUseCase recordResult,
        IMatchesQueryService query)
    {
        _createMatch = createMatch;
        _recordResult = recordResult;
        _query = query;
    }

    // ── POST /matches ─────────────────────────────────────────────────────────
    [HttpPost]
    [IdempotencyFilter]
    public async Task<IActionResult> CreateMatch(
        [FromBody] CreateMatchRequest request,
        CancellationToken ct)
    {
        var result = await _createMatch.ExecuteAsync(request, ct);
        return ApiResponseMapper.ToCreated(result, HttpContext);
    }

    // ── GET /matches ──────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ListMatches(
        [FromQuery] int pageNumber = PageRequest.DefaultPageNumber,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = PageRequest.DefaultSortDirection,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] Guid? teamId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var pageRequest = new PageRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _query.ListAsync(pageRequest, dateFrom, dateTo, teamId, status, ct);
        return ApiResponseMapper.ToPagedOk(result, HttpContext);
    }

    // ── GET /matches/{id} ─────────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMatch(Guid id, CancellationToken ct)
    {
        var match = await _query.GetByIdAsync(id, ct);
        return ApiResponseMapper.ToOkOrNotFound(
            match,
            DomainErrors.MatchNotFound,
            $"Match '{id}' was not found.",
            HttpContext);
    }

    // ── POST /matches/{id}/results ────────────────────────────────────────────
    [HttpPost("{id:guid}/results")]
    [IdempotencyFilter]
    public async Task<IActionResult> RecordResult(
        Guid id,
        [FromBody] RecordMatchResultRequest request,
        CancellationToken ct)
    {
        var result = await _recordResult.ExecuteAsync(id, request, ct);
        return ApiResponseMapper.ToOk(result, HttpContext);
    }
}
