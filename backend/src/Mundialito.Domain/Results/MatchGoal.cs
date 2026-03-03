using Mundialito.Domain.SeedWork;

namespace Mundialito.Domain.Results;

/// <summary>
/// Registro de goles de un jugador en un partido (tabla MatchGoals).
/// </summary>
/// <remarks>
/// Invariantes:
/// - <see cref="Goals"/> debe ser &gt; 0 (no registrar filas con 0 goles).
/// - <see cref="TeamId"/> debe ser home o away del partido; lo valida Application.
/// - <see cref="PlayerId"/> debe pertenecer al <see cref="TeamId"/>; lo valida Application.
/// </remarks>
public sealed class MatchGoal : Entity
{
    /// <summary>Partido al que corresponde el gol.</summary>
    public Guid MatchId { get; private set; }

    /// <summary>Jugador que anotó.</summary>
    public Guid PlayerId { get; private set; }

    /// <summary>Equipo del jugador.</summary>
    public Guid TeamId { get; private set; }

    /// <summary>Cantidad de goles anotados por este jugador en este partido.</summary>
    public int Goals { get; private set; }

    /// <summary>Fecha/hora UTC de registro.</summary>
    public DateTime CreatedAt { get; private set; }

    // Constructor privado para EF Core y la factory.
    private MatchGoal() { }

    // ─── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un registro de goles.
    /// </summary>
    public static Result<MatchGoal> Create(Guid matchId, Guid playerId, Guid teamId, int goals)
    {
        if (matchId == Guid.Empty)
            return Result<MatchGoal>.Fail(DomainErrors.ValidationError, "MatchId is required.");

        if (playerId == Guid.Empty)
            return Result<MatchGoal>.Fail(DomainErrors.ValidationError, "PlayerId is required.");

        if (teamId == Guid.Empty)
            return Result<MatchGoal>.Fail(DomainErrors.ValidationError, "TeamId is required.");

        if (goals <= 0)
            return Result<MatchGoal>.Fail(
                DomainErrors.ValidationError, "Goals must be greater than zero.");

        var goal = new MatchGoal
        {
            MatchId = matchId,
            PlayerId = playerId,
            TeamId = teamId,
            Goals = goals,
            CreatedAt = DateTime.UtcNow
        };
        goal.SetId(Guid.NewGuid());

        return Result<MatchGoal>.Ok(goal);
    }
}
