-- ============================================================
-- SCORERS QUERIES (GET /scorers)
-- ============================================================
-- Agrega goles por jugador desde MatchGoals.
-- Solo jugadores que han anotado ≥ 1 gol (INNER JOIN).
-- Filtros: teamId, search (LIKE FullName).
-- sortBy: goals (TotalGoals) | playerName (p.FullName).
-- Default: goals DESC (más goleadores primero).
-- ============================================================

-- 1. Datos paginados (usando CTE para calcular TotalGoals)
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
ORDER BY TotalGoals DESC   -- o PlayerName ASC según sortBy
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY

-- 2. Total de registros (mismos filtros)
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
