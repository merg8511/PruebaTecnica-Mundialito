namespace Mundialito.Application.DTOs.Results;

/// <summary>
/// Request para POST /matches/{id}/results 
/// { "homeGoals": 2, "awayGoals": 1, "goalsByPlayer": [...] }
/// </summary>
public sealed class RecordMatchResultRequest
{
    /// <summary>Goles del equipo local.</summary>
    public int HomeGoals { get; init; }

    /// <summary>Goles del equipo visitante.</summary>
    public int AwayGoals { get; init; }

    /// <summary>Desglose de goles por jugador.</summary>
    public IReadOnlyList<GoalByPlayerRequest> GoalsByPlayer { get; init; } = [];
}
