namespace Mundialito.Application.Common;

/// <summary>
/// Listas cerradas de campos permitidos para sortBy por endpoint.
/// Fuente de verdad: BLUEPRINT.md §sortBy PERMITIDO.
/// La validación de PAGINATION_INVALID usa estas constantes.
/// </summary>
public static class SortByFields
{
    // ─── Teams ────────────────────────────────────────────────────────────────
    /// <summary>Campos de ordenación permitidos para GET /teams.</summary>
    public static readonly IReadOnlySet<string> Teams = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "createdAt"
    };

    // ─── Players ──────────────────────────────────────────────────────────────
    /// <summary>Campos de ordenación permitidos para GET /players y GET /teams/{teamId}/players.</summary>
    public static readonly IReadOnlySet<string> Players = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "fullName",
        "number",
        "createdAt"
    };

    // ─── Matches ──────────────────────────────────────────────────────────────
    /// <summary>Campos de ordenación permitidos para GET /matches.</summary>
    public static readonly IReadOnlySet<string> Matches = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "scheduledAt",
        "status",
        "createdAt"
    };

    // ─── Scorers ──────────────────────────────────────────────────────────────
    /// <summary>Campos de ordenación permitidos para GET /scorers.</summary>
    public static readonly IReadOnlySet<string> Scorers = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "goals",
        "playerName"
    };
}
