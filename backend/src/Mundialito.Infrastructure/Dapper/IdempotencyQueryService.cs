using Dapper;
using Mundialito.Application.Abstractions;

namespace Mundialito.Infrastructure.Dapper;

/// <summary>
/// Servicio de lectura (Dapper) para idempotency keys.
/// CQRS: SOLO read. No usa EF Core.
/// </summary>
public sealed class IdempotencyQueryService : IIdempotencyQueryService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public IdempotencyQueryService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc/>
    public async Task<IdempotencyRecord?> GetByKeyAsync(
        string idempotencyKey,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT RequestHash, ResponseStatusCode, ResponseBody
            FROM   IdempotencyKeys
            WHERE  IdempotencyKey = @Key
            """;

        using var cn = await _connectionFactory.CreateOpenConnectionAsync(ct);

        // Dapper no soporta CancellationToken en QueryFirstOrDefaultAsync directamente;
        // lo pasamos via CommandDefinition.
        var cmd = new CommandDefinition(sql, new { Key = idempotencyKey }, cancellationToken: ct);
        var row = await cn.QueryFirstOrDefaultAsync<IdempotencyRow>(cmd);

        if (row is null) return null;

        return new IdempotencyRecord(row.RequestHash, row.ResponseStatusCode, row.ResponseBody);
    }

    // Clase auxiliar privada para mapeo de Dapper
    private sealed class IdempotencyRow
    {
        public string RequestHash        { get; init; } = default!;
        public int    ResponseStatusCode { get; init; }
        public string ResponseBody       { get; init; } = default!;
    }
}
