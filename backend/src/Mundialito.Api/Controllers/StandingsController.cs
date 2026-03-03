using Microsoft.AspNetCore.Mvc;
using Mundialito.Api.Mapping;
using Mundialito.Application.Abstractions.QueryServices;
using Mundialito.Application.Common;

namespace Mundialito.Api.Controllers;

/// <summary>
/// GET /standings → orden fijo: points desc, goalDifference desc, goalsFor desc.
/// La lista se envuelve en PaginationResponse (page=1, size=count, total=count, pages=1)
/// según lo requerido por BLUEPRINT: "envelope, aunque sea pequeño".
/// </summary>
[ApiController]
[Route("standings")]
public sealed class StandingsController : ControllerBase
{
    private readonly IStandingsQueryService _query;

    public StandingsController(IStandingsQueryService query)
        => _query = query;

    // ── GET /standings ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetStandings(CancellationToken ct)
    {
        var standings = await _query.ListAsync(ct);

        // Envolver en PaginationResponse: toda la tabla es "una página".
        var response = PaginationResponse<object>.Create(
            standings.Cast<object>().ToList(),
            pageNumber: 1,
            pageSize: standings.Count,
            totalRecords: standings.Count);

        return ApiResponseMapper.ToOkDirect(response);
    }
}
