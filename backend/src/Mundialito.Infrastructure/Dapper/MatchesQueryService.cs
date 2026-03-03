using System.Data;
using Dapper;
using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.QueryServices;
using Mundialito.Application.Common;
using Mundialito.Application.DTOs.Matches;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Infrastructure.Dapper;

/// <summary>
/// Servicio de consulta de partidos usando Dapper (read-only).
/// Filtros soportados:
///   - dateFrom / dateTo → rango en ScheduledAt
///   - teamId → equipo local O visitante
///   - status → whitelist: "Scheduled" | "Played" (case-insensitive).
///              Si el valor no está en la whitelist → Result.Fail(PAGINATION_INVALID). Sin excepciones.
/// sortBy permitido: scheduledAt | status | createdAt.
///   - sortBy null → default (m.ScheduledAt ASC).
///   - sortBy valor inválido → Result.Fail(PAGINATION_INVALID).
/// Paginación: OFFSET/FETCH. Count: SELECT COUNT(1) con mismos filtros.
/// </summary>
public sealed class MatchesQueryService : IMatchesQueryService
{
    // ─── Mapeo seguro sortBy → columna SQL ────────────────────────────────────
    private static readonly Dictionary<string, string> SortColumnMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["scheduledAt"] = "m.ScheduledAt",
            ["status"] = "m.Status",
            ["createdAt"] = "m.CreatedAt"
        };

    private const string DefaultSortColumn = "m.ScheduledAt";
    private const string DefaultSortDir = "ASC";

    private readonly IDbConnectionFactory _connectionFactory;

    public MatchesQueryService(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    /// <inheritdoc/>
    public async Task<Result<PaginationResponse<MatchListItemResponse>>> ListAsync(
        PageRequest pageRequest,
        DateTime? dateFrom,
        DateTime? dateTo,
        Guid? teamId,
        string? status,
        CancellationToken ct = default)
    {
        // ── Validación central: pageRequest + sortBy + status ─────────────────
        // PaginationGuards.Validate valida página/sortBy Y el whitelist de status.
        // Devuelve Result.Fail(PAGINATION_INVALID) — sin lanzar ninguna excepción.
        var validation = PaginationGuards.Validate(pageRequest, SortByFields.Matches, status);
        if (validation.IsFailure)
            return Result<PaginationResponse<MatchListItemResponse>>.Fail(
                validation.ErrorCode!, validation.ErrorMessage!);

        // ── ORDER BY: solo del diccionario (nunca input directo) ──────────────
        var sortColumn = pageRequest.SortBy is not null
            ? SortColumnMap[pageRequest.SortBy]
            : DefaultSortColumn;
        var sortDir = pageRequest.SortBy is not null
            ? (pageRequest.IsDescending ? "DESC" : "ASC")
            : DefaultSortDir;

        // ── Query SQL: datos paginados ────────────────────────────────────────
        // status se pasa como @Status (parámetro), nunca concatenado en la cadena SQL.
        var dataSql = $"""
            SELECT
                m.Id,
                m.HomeTeamId,
                ht.Name  AS HomeTeamName,
                m.AwayTeamId,
                at2.Name AS AwayTeamName,
                m.ScheduledAt,
                m.Status,
                mr.HomeGoals,
                mr.AwayGoals
            FROM Matches m
            INNER JOIN Teams ht  ON ht.Id  = m.HomeTeamId
            INNER JOIN Teams at2 ON at2.Id = m.AwayTeamId
            LEFT  JOIN MatchResults mr ON mr.MatchId = m.Id
            WHERE (@DateFrom IS NULL OR m.ScheduledAt >= @DateFrom)
              AND (@DateTo   IS NULL OR m.ScheduledAt <= @DateTo)
              AND (@TeamId   IS NULL OR m.HomeTeamId = @TeamId OR m.AwayTeamId = @TeamId)
              AND (@Status   IS NULL OR m.Status = @Status)
            ORDER BY {sortColumn} {sortDir}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        // ── Query SQL: total de registros (mismos filtros exactos) ────────────
        const string countSql = """
            SELECT COUNT(1)
            FROM Matches m
            WHERE (@DateFrom IS NULL OR m.ScheduledAt >= @DateFrom)
              AND (@DateTo   IS NULL OR m.ScheduledAt <= @DateTo)
              AND (@TeamId   IS NULL OR m.HomeTeamId = @TeamId OR m.AwayTeamId = @TeamId)
              AND (@Status   IS NULL OR m.Status = @Status)
            """;

        var parameters = new
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            TeamId = teamId,
            Status = status,
            Offset = pageRequest.Offset,
            PageSize = pageRequest.PageSize
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var data = (await connection.QueryAsync<MatchListItemResponse>(
            new CommandDefinition(dataSql, parameters, cancellationToken: ct))).AsList();

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        return Result<PaginationResponse<MatchListItemResponse>>.Ok(
            PaginationResponse<MatchListItemResponse>.Create(
                data, pageRequest.PageNumber, pageRequest.PageSize, total));
    }

    /// <inheritdoc/>
    public async Task<MatchDetailResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                m.Id,
                m.HomeTeamId,
                ht.Name  AS HomeTeamName,
                m.AwayTeamId,
                at2.Name AS AwayTeamName,
                m.ScheduledAt,
                m.Status,
                m.CreatedAt,
                mr.HomeGoals,
                mr.AwayGoals
            FROM Matches m
            INNER JOIN Teams ht  ON ht.Id  = m.HomeTeamId
            INNER JOIN Teams at2 ON at2.Id = m.AwayTeamId
            LEFT  JOIN MatchResults mr ON mr.MatchId = m.Id
            WHERE m.Id = @Id
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        return await connection.QueryFirstOrDefaultAsync<MatchDetailResponse>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
