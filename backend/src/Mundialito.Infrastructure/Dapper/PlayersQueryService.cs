using System.Data;
using Dapper;
using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.QueryServices;
using Mundialito.Application.Common;
using Mundialito.Application.DTOs.Players;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Infrastructure.Dapper;

/// <summary>
/// Servicio de consulta de jugadores usando Dapper (read-only).
/// Filtros soportados: teamId (igualdad), search (LIKE FullName).
/// sortBy permitido: fullName → p.FullName | number → p.Number | createdAt → p.CreatedAt.
/// Paginación: OFFSET/FETCH. Count: SELECT COUNT(1).
/// sortBy null → default (p.CreatedAt ASC).
/// sortBy con valor inválido → Result.Fail(PAGINATION_INVALID).
/// </summary>
public sealed class PlayersQueryService : IPlayersQueryService
{
    // ─── Mapeo seguro sortBy → columna SQL ────────────────────────────────────
    private static readonly Dictionary<string, string> SortColumnMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["fullName"] = "p.FullName",
            ["number"] = "p.Number",
            ["createdAt"] = "p.CreatedAt"
        };

    private const string DefaultSortColumn = "p.CreatedAt";
    private const string DefaultSortDir = "ASC";

    private readonly IDbConnectionFactory _connectionFactory;

    public PlayersQueryService(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    /// <inheritdoc/>
    public async Task<Result<PaginationResponse<PlayerResponse>>> ListAsync(
        PageRequest pageRequest,
        Guid? teamId,
        string? search,
        CancellationToken ct = default)
    {
        // ── Validación central ────────────────────────────────────────────────
        var validation = PaginationGuards.Validate(pageRequest, SortByFields.Players);
        if (validation.IsFailure)
            return Result<PaginationResponse<PlayerResponse>>.Fail(
                validation.ErrorCode!, validation.ErrorMessage!);

        var sortColumn = pageRequest.SortBy is not null
            ? SortColumnMap[pageRequest.SortBy]
            : DefaultSortColumn;
        var sortDir = pageRequest.SortBy is not null
            ? (pageRequest.IsDescending ? "DESC" : "ASC")
            : DefaultSortDir;

        // ── Query SQL: datos paginados ────────────────────────────────────────
        var dataSql = $"""
            SELECT p.Id, p.TeamId, p.FullName, p.Number, p.CreatedAt
            FROM Players p
            WHERE (@TeamId IS NULL OR p.TeamId = @TeamId)
              AND (@Search IS NULL OR p.FullName LIKE '%' + @Search + '%')
            ORDER BY {sortColumn} {sortDir}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        // ── Query SQL: total de registros (mismos filtros exactos) ────────────
        const string countSql = """
            SELECT COUNT(1)
            FROM Players p
            WHERE (@TeamId IS NULL OR p.TeamId = @TeamId)
              AND (@Search IS NULL OR p.FullName LIKE '%' + @Search + '%')
            """;

        var parameters = new
        {
            TeamId = teamId,
            Search = search,
            Offset = pageRequest.Offset,
            PageSize = pageRequest.PageSize
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var data = (await connection.QueryAsync<PlayerResponse>(
            new CommandDefinition(dataSql, parameters, cancellationToken: ct))).AsList();

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        return Result<PaginationResponse<PlayerResponse>>.Ok(
            PaginationResponse<PlayerResponse>.Create(
                data, pageRequest.PageNumber, pageRequest.PageSize, total));
    }

    /// <inheritdoc/>
    public async Task<PlayerResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT p.Id, p.TeamId, p.FullName, p.Number, p.CreatedAt
            FROM Players p
            WHERE p.Id = @Id
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        return await connection.QueryFirstOrDefaultAsync<PlayerResponse>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
