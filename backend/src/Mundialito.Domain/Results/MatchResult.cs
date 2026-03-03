using Mundialito.Domain.Events;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Domain.Results;

/// <summary>
/// Resultado oficial de un partido (tabla MatchResults).
/// Entidad de solo creación: los resultados no se modifican una vez registrados.
/// </summary>
/// <remarks>
/// Invariantes:
/// - <see cref="HomeGoals"/> y <see cref="AwayGoals"/> deben ser >= 0.
/// - La consistencia entre este marcador y la suma de goles por jugador
///   se valida en el Command Handler de Application.
/// </remarks>
public sealed class MatchResult : Entity
{
    /// <summary>Partido al que pertenece el resultado.</summary>
    public Guid MatchId { get; private set; }

    /// <summary>Goles del equipo local.</summary>
    public int HomeGoals { get; private set; }

    /// <summary>Goles del equipo visitante.</summary>
    public int AwayGoals { get; private set; }

    /// <summary>Fecha/hora UTC en que se registró el resultado.</summary>
    public DateTime RecordedAt { get; private set; }

    // Constructor privado para EF Core y la factory.
    private MatchResult() { }

    // ─── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea el resultado de un partido.
    /// </summary>
    public static Result<MatchResult> Create(Guid matchId, int homeGoals, int awayGoals)
    {
        if (matchId == Guid.Empty)
            return Result<MatchResult>.Fail(DomainErrors.ValidationError, "MatchId is required.");

        if (homeGoals < 0)
            return Result<MatchResult>.Fail(
                DomainErrors.ValidationError, "HomeGoals cannot be negative.");

        if (awayGoals < 0)
            return Result<MatchResult>.Fail(
                DomainErrors.ValidationError, "AwayGoals cannot be negative.");

        var result = new MatchResult
        {
            MatchId = matchId,
            HomeGoals = homeGoals,
            AwayGoals = awayGoals,
            RecordedAt = DateTime.UtcNow
        };
        result.SetId(Guid.NewGuid());

        return Result<MatchResult>.Ok(result);
    }

    // ─── Domain Event ─────────────────────────────────────────────────────────

    /// <summary>
    /// Registra el evento <see cref="MatchResultRecordedEvent"/> en la entidad.
    /// Lo llama el Command Handler de Application tras validar el resultado.
    /// Infrastructure lo despachará tras commitear el UoW.
    /// </summary>
    public void RegisterResultRecordedEvent(Guid matchId, int homeGoals, int awayGoals) =>
        AddDomainEvent(new MatchResultRecordedEvent(matchId, homeGoals, awayGoals));
}
