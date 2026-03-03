-- ============================================================
-- MATCHES QUERIES (GET /matches, GET /matches/{id})
-- ============================================================

-- 1. Datos paginados
--    Filtros: dateFrom/dateTo (ScheduledAt), teamId (home OR away),
--             status (whitelist: Scheduled|Played — parámetro, nunca concatenado)
--    sortBy: scheduledAt|status|createdAt
--    JOIN MatchResults LEFT para incluir goles (null si Scheduled)
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
ORDER BY m.ScheduledAt ASC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY

-- 2. Total de registros (mismos filtros exactos)
SELECT COUNT(1)
FROM Matches m
WHERE (@DateFrom IS NULL OR m.ScheduledAt >= @DateFrom)
  AND (@DateTo   IS NULL OR m.ScheduledAt <= @DateTo)
  AND (@TeamId   IS NULL OR m.HomeTeamId = @TeamId OR m.AwayTeamId = @TeamId)
  AND (@Status   IS NULL OR m.Status = @Status)

-- 3. Detalle por Id (GET /matches/{id})
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
