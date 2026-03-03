using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Common;

/// <summary>
/// Guards de validación centralizados para el read side (Query Services).
/// Evita duplicar lógica de validación de filtros en cada QueryService.
/// NUNCA lanza excepciones; siempre devuelve Result(PAGINATION_INVALID) si el input es inválido.
/// </summary>
public static class PaginationGuards
{
    // Whitelist de valores válidos para el filtro "status" en partidos.
    private static readonly string[] ValidMatchStatusesList = ["Scheduled", "Played"];
    private static readonly HashSet<string> ValidMatchStatuses = new(ValidMatchStatusesList, StringComparer.OrdinalIgnoreCase);



    /// <summary>
    /// Valida los parámetros de paginación/orden y, opcionalmente, el filtro de status de partido.
    /// </summary>
    /// <param name="pageRequest">Parámetros de paginación y ordenación.</param>
    /// <param name="allowedSortFields">Campos de sortBy permitidos para el endpoint.</param>
    /// <param name="status">Filtro de status a validar (solo para /matches). Null = omitido.</param>
    /// <returns>Result exitoso si todo es válido; Result.Fail(PAGINATION_INVALID) si no.</returns>
    public static Result Validate(
        PageRequest pageRequest,
        IReadOnlySet<string> allowedSortFields,
        string? status = null)
    {
        // 1. Validar pageNumber / pageSize / sortDirection / sortBy
        var pageResult = pageRequest.Validate(allowedSortFields);
        if (pageResult.IsFailure)
            return pageResult;

        // 2. Validar status (solo cuando se proporciona un valor)
        if (status is not null && !ValidMatchStatuses.Contains(status))
            return Result.Fail(
                DomainErrors.PaginationInvalid,
                $"'status' value '{status}' is not allowed. Allowed values: {string.Join(", ", ValidMatchStatusesList)}.");

        return Result.Ok();
    }
}
