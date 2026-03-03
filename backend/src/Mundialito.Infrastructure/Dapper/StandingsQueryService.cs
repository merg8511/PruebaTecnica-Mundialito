using System.Data;
using Dapper;
using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.QueryServices;
using Mundialito.Application.DTOs.Standings;

namespace Mundialito.Infrastructure.Dapper;

/// <summary>
/// Servicio de consulta de clasificación usando Dapper (read-only).
/// Sin paginación ni sortBy: la clasificación usa orden FIJO obligatorio per BLUEPRINT:
///   points DESC → goalDifference DESC → goalsFor DESC.
///
/// Cálculo agregado por equipo (local + visitante via UNION ALL):
///   played, wins, draws, losses, goalsFor, goalsAgainst, goalDifference, points.
/// Equipos sin partidos aparecen con estadísticas en 0 (LEFT JOIN).
/// </summary>
public sealed class StandingsQueryService : IStandingsQueryService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public StandingsQueryService(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    /// <inheritdoc/>
    /// <remarks>
    /// Orden fijo: points DESC, goalDifference DESC, goalsFor DESC.
    /// </remarks>
    public async Task<IReadOnlyList<StandingItemResponse>> ListAsync(CancellationToken ct = default)
    {
        // ── Cálculo agregado completo en SQL ──────────────────────────────────
        const string sql = """
            WITH TeamMatches AS (
                -- Partido como equipo LOCAL (home)
                SELECT
                    m.HomeTeamId       AS TeamId,
                    mr.HomeGoals       AS GoalsFor,
                    mr.AwayGoals       AS GoalsAgainst,
                    CASE WHEN mr.HomeGoals > mr.AwayGoals THEN 1 ELSE 0 END AS IsWin,
                    CASE WHEN mr.HomeGoals = mr.AwayGoals THEN 1 ELSE 0 END AS IsDraw,
                    CASE WHEN mr.HomeGoals < mr.AwayGoals THEN 1 ELSE 0 END AS IsLoss
                FROM Matches m
                INNER JOIN MatchResults mr ON mr.MatchId = m.Id
                WHERE m.Status = 'Played'

                UNION ALL

                -- Partido como equipo VISITANTE (away)
                SELECT
                    m.AwayTeamId       AS TeamId,
                    mr.AwayGoals       AS GoalsFor,
                    mr.HomeGoals       AS GoalsAgainst,
                    CASE WHEN mr.AwayGoals > mr.HomeGoals THEN 1 ELSE 0 END AS IsWin,
                    CASE WHEN mr.AwayGoals = mr.HomeGoals THEN 1 ELSE 0 END AS IsDraw,
                    CASE WHEN mr.AwayGoals < mr.HomeGoals THEN 1 ELSE 0 END AS IsLoss
                FROM Matches m
                INNER JOIN MatchResults mr ON mr.MatchId = m.Id
                WHERE m.Status = 'Played'
            )
            SELECT
                t.Id                              AS TeamId,
                t.Name                            AS TeamName,
                COALESCE(COUNT(tm.TeamId),     0) AS Played,
                COALESCE(SUM(tm.IsWin),        0) AS Wins,
                COALESCE(SUM(tm.IsDraw),       0) AS Draws,
                COALESCE(SUM(tm.IsLoss),       0) AS Losses,
                COALESCE(SUM(tm.GoalsFor),     0) AS GoalsFor,
                COALESCE(SUM(tm.GoalsAgainst), 0) AS GoalsAgainst,
                COALESCE(SUM(tm.GoalsFor) - SUM(tm.GoalsAgainst), 0) AS GoalDifference,
                COALESCE(SUM(tm.IsWin) * 3 + SUM(tm.IsDraw),     0) AS Points
            FROM Teams t
            LEFT JOIN TeamMatches tm ON tm.TeamId = t.Id
            GROUP BY t.Id, t.Name
            ORDER BY
                Points         DESC,
                GoalDifference DESC,
                GoalsFor       DESC
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var results = await connection.QueryAsync<StandingItemResponse>(
            new CommandDefinition(sql, cancellationToken: ct));

        return results.AsList();
    }
}
