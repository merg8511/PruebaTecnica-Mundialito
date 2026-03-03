namespace Mundialito.Application.DTOs.Results;

/// <summary>
/// Respuesta de POST /matches/{id}/results 
/// { "matchId", "homeGoals", "awayGoals", "recordedAt", "goalsByPlayer": [...] }
/// </summary>
public sealed class MatchResultResponse
{
    public Guid     MatchId       { get; init; }
    public int      HomeGoals     { get; init; }
    public int      AwayGoals     { get; init; }
    public DateTime RecordedAt    { get; init; }
    public IReadOnlyList<GoalByPlayerResponse> GoalsByPlayer { get; init; } = [];
}
