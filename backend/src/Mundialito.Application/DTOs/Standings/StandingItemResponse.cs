namespace Mundialito.Application.DTOs.Standings;

/// <summary>
/// Ítem de clasificación para GET /standings según BLUEPRINT:
/// { "teamId", "teamName", "played", "wins", "draws", "losses",
///   "goalsFor", "goalsAgainst", "goalDifference", "points" }
/// Orden obligatorio: points desc, goalDifference desc, goalsFor desc.
/// </summary>
public sealed class StandingItemResponse
{
    public Guid   TeamId         { get; init; }
    public string TeamName       { get; init; } = string.Empty;
    public int    Played         { get; init; }
    public int    Wins           { get; init; }
    public int    Draws          { get; init; }
    public int    Losses         { get; init; }
    public int    GoalsFor       { get; init; }
    public int    GoalsAgainst   { get; init; }
    public int    GoalDifference { get; init; }
    public int    Points         { get; init; }
}
