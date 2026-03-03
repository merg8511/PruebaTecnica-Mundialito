using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Common;

/// <summary>
/// Modelo de petición de paginación y ordenación estandarizado.
/// Los controladores mapean query params a esta clase.
/// La validación contra listas permitidas de sortBy se realiza
/// a través de <see cref="SortByFields"/>.
/// </summary>
public sealed class PageRequest
{
    // ─── Defaults ─────────────────────────────────────────────────────────────
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;
    public const string DefaultSortDirection = "asc";

    // ─── Propiedades ──────────────────────────────────────────────────────────

    /// <summary>Número de página (1-based). Mínimo 1.</summary>
    public int PageNumber { get; init; } = DefaultPageNumber;

    /// <summary>Registros por página. Rango [1, 100].</summary>
    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>Campo de ordenación. Validar contra la lista permitida del endpoint.</summary>
    public string? SortBy { get; init; }

    /// <summary>"asc" o "desc". Default "asc".</summary>
    public string SortDirection { get; init; } = DefaultSortDirection;

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Offset para SQL: (PageNumber - 1) * PageSize.</summary>
    public int Offset => (PageNumber - 1) * PageSize;

    /// <summary>true si SortDirection es "desc" (case-insensitive).</summary>
    public bool IsDescending =>
        string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

    // ─── Validación central ───────────────────────────────────────────────────

    /// <summary>
    /// Valida los parámetros de paginación y ordenación.
    /// <para>Reglas:</para>
    /// <list type="bullet">
    ///   <item>PageNumber ≥ 1</item>
    ///   <item>PageSize entre 1 y 100</item>
    ///   <item>SortDirection debe ser "asc" o "desc" (case-insensitive)</item>
    ///   <item>SortBy, si se especifica, debe estar en <paramref name="allowedSortFields"/></item>
    /// </list>
    /// </summary>
    /// <param name="allowedSortFields">Set de campos de ordenación permitidos para el endpoint.</param>
    /// <returns>Result exitoso si todo es válido; Result.Fail(PAGINATION_INVALID) si no.</returns>
    public Result Validate(IReadOnlySet<string> allowedSortFields)
    {
        if (PageNumber < 1)
            return Result.Fail(DomainErrors.PaginationInvalid,
                $"'pageNumber' must be ≥ 1 (got {PageNumber}).");

        if (PageSize < 1 || PageSize > MaxPageSize)
            return Result.Fail(DomainErrors.PaginationInvalid,
                $"'pageSize' must be between 1 and {MaxPageSize} (got {PageSize}).");

        if (!string.Equals(SortDirection, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            return Result.Fail(DomainErrors.PaginationInvalid,
                $"'sortDirection' must be 'asc' or 'desc' (got '{SortDirection}').");

        if (SortBy is not null && !allowedSortFields.Contains(SortBy))
            return Result.Fail(DomainErrors.PaginationInvalid,
                $"'sortBy' value '{SortBy}' is not allowed. Allowed values: {string.Join(", ", allowedSortFields)}.");

        return Result.Ok();
    }
}
