using System.Data;
using Dapper;
using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.QueryServices;
using Mundialito.Application.Common;
using Mundialito.Application.DTOs.Scorers;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Infrastructure.Dapper;

/// <summary>
/// Servicio de consulta de goleadores usando Dapper (read-only).
/// Agrega goles por jugador (SUM) desde la tabla MatchGoals.
/// Solo incluye jugadores con ≥ 1 gol (INNER JOIN MatchGoals).
/// Filtros soportados: teamId (igualdad), search (LIKE FullName).
/// sortBy permitido: goals → TotalGoals | playerName → PlayerName.
///   - sortBy null → default (TotalGoals DESC).
///   - sortBy valor inválido → Result.Fail(PAGINATION_INVALID).
/// Paginación: OFFSET/FETCH sobre CTE. Count: SELECT COUNT(1) con mismo filtro.
/// </summary>
public sealed class ScorersQueryService : IScorersQueryService
{
    // ─── Mapeo seguro sortBy → alias de columna del CTE ───────────────────────
    private static readonly Dictionary<string, string> SortColumnMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["goals"] = "TotalGoals",
            ["playerName"] = "PlayerName"
        };

    // Default: más goleadores primero
    private const string DefaultSortColumn = "TotalGoals";
    private const string DefaultSortDir = "DESC";

    private readonly IDbConnectionFactory _connectionFactory;

    public ScorersQueryService(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    /// <inheritdoc/>
    public async Task<Result<PaginationResponse<ScorerItemResponse>>> ListAsync(
        PageRequest pageRequest,
        Guid? teamId,
        string? search,
        CancellationToken ct = default)
    {
        // ── Validación central ────────────────────────────────────────────────
        var validation = PaginationGuards.Validate(pageRequest, SortByFields.Scorers);
        if (validation.IsFailure)
            return Result<PaginationResponse<ScorerItemResponse>>.Fail(
                validation.ErrorCode!, validation.ErrorMessage!);

        // ── ORDER BY: sortBy null → default DESC; sortBy válido → respeta sortDirection ──
        var sortColumn = pageRequest.SortBy is not null
            ? SortColumnMap[pageRequest.SortBy]
            : DefaultSortColumn;
        var sortDir = pageRequest.SortBy is not null
            ? (pageRequest.IsDescending ? "DESC" : "ASC")
            : DefaultSortDir;   // cuando no se pide sort explícito, default es goals DESC

        // ── CTE + paginación ──────────────────────────────────────────────────
        var dataSql = $"""
            WITH ScorerAgg AS (
                SELECT
                    p.Id          AS PlayerId,
                    p.FullName    AS PlayerName,
                    p.TeamId,
                    t.Name        AS TeamName,
                    SUM(mg.Goals) AS TotalGoals
                FROM Players p
                INNER JOIN Teams t       ON t.Id = p.TeamId
                INNER JOIN MatchGoals mg ON mg.PlayerId = p.Id
                WHERE (@TeamId IS NULL OR p.TeamId = @TeamId)
                  AND (@Search IS NULL OR p.FullName LIKE '%' + @Search + '%')
                GROUP BY p.Id, p.FullName, p.TeamId, t.Name
            )
            SELECT
                PlayerId,
                PlayerName,
                TeamId,
                TeamName,
                TotalGoals AS Goals
            FROM ScorerAgg
            ORDER BY {sortColumn} {sortDir}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        // ── Count: mismos filtros sin paginación ──────────────────────────────
        const string countSql = """
            SELECT COUNT(1)
            FROM (
                SELECT p.Id
                FROM Players p
                INNER JOIN Teams t       ON t.Id = p.TeamId
                INNER JOIN MatchGoals mg ON mg.PlayerId = p.Id
                WHERE (@TeamId IS NULL OR p.TeamId = @TeamId)
                  AND (@Search IS NULL OR p.FullName LIKE '%' + @Search + '%')
                GROUP BY p.Id
            ) AS agg
            """;

        var parameters = new
        {
            TeamId = teamId,
            Search = search,
            Offset = pageRequest.Offset,
            PageSize = pageRequest.PageSize
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var data = (await connection.QueryAsync<ScorerItemResponse>(
            new CommandDefinition(dataSql, parameters, cancellationToken: ct))).AsList();

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        return Result<PaginationResponse<ScorerItemResponse>>.Ok(
            PaginationResponse<ScorerItemResponse>.Create(
                data, pageRequest.PageNumber, pageRequest.PageSize, total));
    }
}
