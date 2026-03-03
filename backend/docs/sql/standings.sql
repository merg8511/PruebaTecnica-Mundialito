-- ============================================================
-- STANDINGS QUERY (GET /standings)
-- ============================================================
-- Sin paginación. Orden FIJO: points DESC, goalDifference DESC, goalsFor DESC.
-- Agrega partidos como LOCAL y VISITANTE via UNION ALL.
-- Equipos sin partidos jugados aparecen con estadísticas en 0.
-- ============================================================

WITH TeamMatches AS (
    -- Como equipo LOCAL
    SELECT
        m.HomeTeamId  AS TeamId,
        mr.HomeGoals  AS GoalsFor,
        mr.AwayGoals  AS GoalsAgainst,
        CASE WHEN mr.HomeGoals > mr.AwayGoals THEN 1 ELSE 0 END AS IsWin,
        CASE WHEN mr.HomeGoals = mr.AwayGoals THEN 1 ELSE 0 END AS IsDraw,
        CASE WHEN mr.HomeGoals < mr.AwayGoals THEN 1 ELSE 0 END AS IsLoss
    FROM Matches m
    INNER JOIN MatchResults mr ON mr.MatchId = m.Id
    WHERE m.Status = 'Played'

    UNION ALL

    -- Como equipo VISITANTE
    SELECT
        m.AwayTeamId  AS TeamId,
        mr.AwayGoals  AS GoalsFor,
        mr.HomeGoals  AS GoalsAgainst,
        CASE WHEN mr.AwayGoals > mr.HomeGoals THEN 1 ELSE 0 END AS IsWin,
        CASE WHEN mr.AwayGoals = mr.HomeGoals THEN 1 ELSE 0 END AS IsDraw,
        CASE WHEN mr.AwayGoals < mr.HomeGoals THEN 1 ELSE 0 END AS IsLoss
    FROM Matches m
    INNER JOIN MatchResults mr ON mr.MatchId = m.Id
    WHERE m.Status = 'Played'
)
SELECT
    t.Id                          AS TeamId,
    t.Name                        AS TeamName,
    COALESCE(COUNT(tm.TeamId), 0) AS Played,
    COALESCE(SUM(tm.IsWin),  0)   AS Wins,
    COALESCE(SUM(tm.IsDraw), 0)   AS Draws,
    COALESCE(SUM(tm.IsLoss), 0)   AS Losses,
    COALESCE(SUM(tm.GoalsFor),     0) AS GoalsFor,
    COALESCE(SUM(tm.GoalsAgainst), 0) AS GoalsAgainst,
    COALESCE(SUM(tm.GoalsFor) - SUM(tm.GoalsAgainst), 0) AS GoalDifference,
    COALESCE(SUM(tm.IsWin) * 3 + SUM(tm.IsDraw), 0)     AS Points
FROM Teams t
LEFT JOIN TeamMatches tm ON tm.TeamId = t.Id
GROUP BY t.Id, t.Name
ORDER BY
    Points         DESC,
    GoalDifference DESC,
    GoalsFor       DESC
