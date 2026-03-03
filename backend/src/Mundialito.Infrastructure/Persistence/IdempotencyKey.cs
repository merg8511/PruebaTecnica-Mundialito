namespace Mundialito.Infrastructure.Persistence;

/// <summary>
/// Registro de idempotencia para POST endpoints.
/// Estructura mínima alineada .
/// </summary>
public sealed class IdempotencyKey
{
    public Guid   Id                 { get; set; }
    public string IdempotencyKeyValue { get; set; } = default!;
    public string RequestHash        { get; set; } = default!;
    public int    ResponseStatusCode { get; set; }
    public string ResponseBody       { get; set; } = default!;
    public DateTime CreatedAt        { get; set; }
}
