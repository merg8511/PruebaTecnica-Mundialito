-- ============================================================
-- PLAYERS QUERIES (GET /players, GET /teams/{teamId}/players)
-- ============================================================

-- 1. Datos paginados
--    Filtros: teamId (igualdad), search (LIKE FullName)
--    sortBy: fullName|number|createdAt
SELECT p.Id, p.TeamId, p.FullName, p.Number, p.CreatedAt
FROM Players p
WHERE (@TeamId IS NULL OR p.TeamId = @TeamId)
  AND (@Search IS NULL OR p.FullName LIKE '%' + @Search + '%')
ORDER BY p.CreatedAt ASC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY

-- 2. Total de registros (mismos filtros exactos)
SELECT COUNT(1)
FROM Players p
WHERE (@TeamId IS NULL OR p.TeamId = @TeamId)
  AND (@Search IS NULL OR p.FullName LIKE '%' + @Search + '%')

-- 3. Detalle por Id (GET /players/{id})
SELECT p.Id, p.TeamId, p.FullName, p.Number, p.CreatedAt
FROM Players p
WHERE p.Id = @Id
