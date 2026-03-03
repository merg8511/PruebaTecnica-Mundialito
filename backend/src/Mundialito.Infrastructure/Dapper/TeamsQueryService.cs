using System.Data;
using Dapper;
using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.QueryServices;
using Mundialito.Application.Common;
using Mundialito.Application.DTOs.Teams;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Infrastructure.Dapper;

/// <summary>
/// Servicio de consulta de equipos usando Dapper (read-only).
/// Filtros soportados: search (LIKE Name).
/// sortBy permitido: name → t.Name | createdAt → t.CreatedAt.
/// Paginación: OFFSET/FETCH. Count: SELECT COUNT(1).
/// sortBy null → default (t.CreatedAt ASC).
/// sortBy con valor inválido → Result.Fail(PAGINATION_INVALID).
/// </summary>
public sealed class TeamsQueryService : ITeamsQueryService
{
    // ─── Mapeo seguro sortBy → columna SQL ────────────────────────────────────
    // Solo los campos de este diccionario se usan en ORDER BY (nunca input directo).
    private static readonly Dictionary<string, string> SortColumnMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = "t.Name",
            ["createdAt"] = "t.CreatedAt"
        };

    private const string DefaultSortColumn = "t.CreatedAt";
    private const string DefaultSortDir = "ASC";

    private readonly IDbConnectionFactory _connectionFactory;

    public TeamsQueryService(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    /// <inheritdoc/>
    public async Task<Result<PaginationResponse<TeamResponse>>> ListAsync(
        PageRequest pageRequest,
        string? search,
        CancellationToken ct = default)
    {
        // ── Validación central (pageNumber/pageSize/sortDirection/sortBy) ─────
        var validation = PaginationGuards.Validate(pageRequest, SortByFields.Teams);
        if (validation.IsFailure)
            return Result<PaginationResponse<TeamResponse>>.Fail(
                validation.ErrorCode!, validation.ErrorMessage!);

        // ── Columna ORDER BY: solo del diccionario (nunca input directo) ──────
        // sortBy null → default; sortBy válido → su columna; inválido ya fue rechazado arriba.
        var sortColumn = pageRequest.SortBy is not null
            ? SortColumnMap[pageRequest.SortBy]   // key garantizada por Validate
            : DefaultSortColumn;
        var sortDir = pageRequest.SortBy is not null
            ? (pageRequest.IsDescending ? "DESC" : "ASC")
            : DefaultSortDir;

        // ── Query SQL: datos paginados ────────────────────────────────────────
        var dataSql = $"""
            SELECT t.Id, t.Name, t.CreatedAt
            FROM Teams t
            WHERE (@Search IS NULL OR t.Name LIKE '%' + @Search + '%')
            ORDER BY {sortColumn} {sortDir}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        // ── Query SQL: total de registros (mismos filtros exactos) ────────────
        const string countSql = """
            SELECT COUNT(1)
            FROM Teams t
            WHERE (@Search IS NULL OR t.Name LIKE '%' + @Search + '%')
            """;

        var parameters = new
        {
            Search = search,
            Offset = pageRequest.Offset,
            PageSize = pageRequest.PageSize
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var data = (await connection.QueryAsync<TeamResponse>(
            new CommandDefinition(dataSql, parameters, cancellationToken: ct))).AsList();

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        return Result<PaginationResponse<TeamResponse>>.Ok(
            PaginationResponse<TeamResponse>.Create(
                data, pageRequest.PageNumber, pageRequest.PageSize, total));
    }

    /// <inheritdoc/>
    public async Task<TeamResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT t.Id, t.Name, t.CreatedAt
            FROM Teams t
            WHERE t.Id = @Id
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        return await connection.QueryFirstOrDefaultAsync<TeamResponse>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
