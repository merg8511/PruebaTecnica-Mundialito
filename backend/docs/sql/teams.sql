-- ============================================================
-- TEAMS QUERIES (GET /teams)
-- ============================================================

-- 1. Datos paginados (sortBy: name|createdAt; sortDir: asc|desc)
SELECT t.Id, t.Name, t.CreatedAt
FROM Teams t
WHERE (@Search IS NULL OR t.Name LIKE '%' + @Search + '%')
ORDER BY t.CreatedAt ASC   -- columna viene del diccionario, nunca concatenada directo
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY

-- 2. Total de registros (mismos filtros exactos)
SELECT COUNT(1)
FROM Teams t
WHERE (@Search IS NULL OR t.Name LIKE '%' + @Search + '%')

-- 3. Detalle por Id (GET /teams/{id})
SELECT t.Id, t.Name, t.CreatedAt
FROM Teams t
WHERE t.Id = @Id
