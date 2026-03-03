using Microsoft.AspNetCore.Mvc;
using Mundialito.Api.Mapping;
using Mundialito.Application.Abstractions.QueryServices;
using Mundialito.Application.Common;

namespace Mundialito.Api.Controllers;

/// <summary>
/// GET /scorers — listado paginado de goleadores.
/// sortBy: goals | playerName. Filtros: teamId, search.
/// </summary>
[ApiController]
[Route("scorers")]
public sealed class ScorersController : ControllerBase
{
    private readonly IScorersQueryService _query;

    public ScorersController(IScorersQueryService query)
        => _query = query;

    // ── GET /scorers ──────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ListScorers(
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
}
