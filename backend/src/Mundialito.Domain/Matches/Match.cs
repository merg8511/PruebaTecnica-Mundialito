using Mundialito.Domain.SeedWork;

namespace Mundialito.Domain.Matches;

/// <summary>
/// Agregado raíz que representa un partido del torneo.
/// </summary>
/// <remarks>
/// Invariantes de dominio:
/// - <see cref="HomeTeamId"/> y <see cref="AwayTeamId"/> no pueden ser el mismo equipo.
/// - <see cref="ScheduledAt"/> debe ser un instante futuro al momento de creación
///   (la validación de "es futuro" se puede relajar a "no null" en el dominio,
///    dejando la regla de negocio de fechas para Application).
/// - Un partido solo puede pasar a <see cref="MatchStatus.Played"/> una vez
///   (guardado por <see cref="MarkAsPlayed"/>).
/// </remarks>
public sealed class Match : Entity
{
    /// <summary>Equipo local.</summary>
    public Guid HomeTeamId { get; private set; }

    /// <summary>Equipo visitante.</summary>
    public Guid AwayTeamId { get; private set; }

    /// <summary>Fecha/hora UTC programada para el partido.</summary>
    public DateTime ScheduledAt { get; private set; }

    /// <summary>Estado actual del partido.</summary>
    public MatchStatus Status { get; private set; }

    /// <summary>Fecha/hora UTC de registro del partido en el sistema.</summary>
    public DateTime CreatedAt { get; private set; }

    // Constructor privado para EF Core y la factory.
    private Match() { }

    // ─── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo partido en estado <see cref="MatchStatus.Scheduled"/>.
    /// </summary>
    public static Result<Match> Create(Guid homeTeamId, Guid awayTeamId, DateTime scheduledAt)
    {
        if (homeTeamId == Guid.Empty)
            return Result<Match>.Fail(DomainErrors.ValidationError, "HomeTeamId is required.");

        if (awayTeamId == Guid.Empty)
            return Result<Match>.Fail(DomainErrors.ValidationError, "AwayTeamId is required.");

        if (homeTeamId == awayTeamId)
            return Result<Match>.Fail(
                DomainErrors.ValidationError,
                "A team cannot play against itself.");

        if (scheduledAt == default)
            return Result<Match>.Fail(DomainErrors.ValidationError, "ScheduledAt is required.");

        var match = new Match
        {
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            ScheduledAt = scheduledAt.ToUniversalTime(),
            Status = MatchStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };
        match.SetId(Guid.NewGuid());

        return Result<Match>.Ok(match);
    }

    // ─── Métodos de negocio ──────────────────────────────────────────────────

    /// <summary>
    /// Transiciona el partido al estado <see cref="MatchStatus.Played"/>.
    /// Devuelve fallo si ya está jugado (<see cref="DomainErrors.MatchAlreadyPlayed"/>).
    /// </summary>
    /// <remarks>
    /// Este método NO persiste resultados. La persistencia es responsabilidad
    /// del Command Handler en Application + UoW en Infrastructure.
    /// </remarks>
    public Result MarkAsPlayed()
    {
        if (Status == MatchStatus.Played)
            return Result.Fail(
                DomainErrors.MatchAlreadyPlayed,
                "This match has already been played.");

        Status = MatchStatus.Played;
        return Result.Ok();
    }
}
