namespace Mundialito.Application.Common;

/// <summary>
/// Envelope de respuesta paginada 
/// Estructura fija: { data, pageNumber, pageSize, totalRecords, totalPages }
/// </summary>
public sealed class PaginationResponse<T>
{
    /// <summary>Página de datos devuelta.</summary>
    public IReadOnlyList<T> Data { get; init; } = [];

    /// <summary>Número de página solicitado (1-based).</summary>
    public int PageNumber { get; init; }

    /// <summary>Tamaño de página solicitado.</summary>
    public int PageSize { get; init; }

    /// <summary>Total de registros que cumplen el filtro (sin paginar).</summary>
    public int TotalRecords { get; init; }

    /// <summary>Total de páginas calculado: ⌈totalRecords / pageSize⌉.</summary>
    public int TotalPages { get; init; }

    // ─── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un envelope de paginación calculando TotalPages automáticamente.
    /// </summary>
    public static PaginationResponse<T> Create(
        IReadOnlyList<T> data,
        int pageNumber,
        int pageSize,
        int totalRecords)
    {
        var totalPages = pageSize > 0
            ? (int)Math.Ceiling((double)totalRecords / pageSize)
            : 0;

        return new PaginationResponse<T>
        {
            Data = data,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages
        };
    }
}
