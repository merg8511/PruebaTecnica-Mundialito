using System.Data;

namespace Mundialito.Application.Abstractions;

/// <summary>
/// Factoría de conexiones a la base de datos.
/// La implementación (Infrastructure) crea y abre una SqlConnection.
/// Solo el lado de lectura (Dapper) debe usarla.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Crea y abre una conexión a la base de datos.
    /// La conexión devuelta ya está abierta y lista para consultas.
    /// El llamador es responsable de disponer la conexión.
    /// </summary>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
