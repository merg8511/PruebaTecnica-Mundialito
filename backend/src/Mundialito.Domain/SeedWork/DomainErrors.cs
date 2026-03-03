namespace Mundialito.Domain.SeedWork;

/// <summary>
/// Catálogo CERRADO de error codes del sistema.
/// Es el ÚNICO lugar donde se definen; ningún otra clase debe usar strings literales
/// distintos de estas constantes.
///
/// CATÁLOGO FINAL DE ERRORCODES.
/// </summary>
public static class DomainErrors
{
    // ─── 400 Bad Request ──────────────────────────────────────────────────────
    public const string ValidationError = "VALIDATION_ERROR";
    public const string PaginationInvalid = "PAGINATION_INVALID";
    public const string IdempotencyKeyRequired = "IDEMPOTENCY_KEY_REQUIRED";
    public const string MatchResultInconsistent = "MATCH_RESULT_INCONSISTENT";
    public const string PlayerNotInMatch = "PLAYER_NOT_IN_MATCH";

    // ─── 404 Not Found ────────────────────────────────────────────────────────
    public const string TeamNotFound = "TEAM_NOT_FOUND";
    public const string PlayerNotFound = "PLAYER_NOT_FOUND";
    public const string MatchNotFound = "MATCH_NOT_FOUND";

    // ─── 409 Conflict ─────────────────────────────────────────────────────────
    public const string TeamNameConflict = "TEAM_NAME_CONFLICT";
    public const string MatchAlreadyPlayed = "MATCH_ALREADY_PLAYED";
    public const string TeamHasDependencies = "TEAM_HAS_DEPENDENCIES";
    public const string IdempotencyKeyConflict = "IDEMPOTENCY_KEY_CONFLICT";
    public const string ResourceConflict = "RESOURCE_CONFLICT";

    // ─── 500 Internal ─────────────────────────────────────────────────────────
    /// <remarks>Solo lo emite el middleware global de excepciones. Nunca desde handlers.</remarks>
    public const string InternalError = "INTERNAL_ERROR";
}
