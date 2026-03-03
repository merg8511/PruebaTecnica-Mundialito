using Mundialito.Domain.SeedWork;

namespace Mundialito.Domain.Events;

/// <summary>
/// Se dispara cuando el resultado de un partido es registrado exitosamente.
/// Lo levanta el Command Handler de Application tras persistir el resultado,
/// pero el TIPO vive en Domain.
/// </summary>
public sealed class MatchResultRecordedEvent : DomainEvent
{
    public Guid MatchId { get; }
    public int HomeGoals { get; }
    public int AwayGoals { get; }

    public MatchResultRecordedEvent(Guid matchId, int homeGoals, int awayGoals)
    {
        MatchId = matchId;
        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
    }
}
