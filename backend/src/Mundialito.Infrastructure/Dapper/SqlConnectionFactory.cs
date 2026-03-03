using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Mundialito.Application.Abstractions;

namespace Mundialito.Infrastructure.Dapper;

/// <summary>
/// Implementación de IDbConnectionFactory para SQL Server.
/// Lee la cadena de conexión desde IConfiguration ("DefaultConnection").
/// Abre la conexión antes de devolverla; soporta CancellationToken.
/// </summary>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");
    }

    /// <inheritdoc/>
    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
