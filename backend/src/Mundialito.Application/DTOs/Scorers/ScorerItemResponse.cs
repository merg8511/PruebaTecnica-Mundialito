namespace Mundialito.Application.DTOs.Scorers;

/// <summary>
/// Ítem de goleador para GET /scorers según BLUEPRINT:
/// { "playerId", "playerName", "teamId", "teamName", "goals" }
/// </summary>
public sealed class ScorerItemResponse
{
    public Guid   PlayerId   { get; init; }
    public string PlayerName { get; init; } = string.Empty;
    public Guid   TeamId     { get; init; }
    public string TeamName   { get; init; } = string.Empty;
    public int    Goals      { get; init; }
}
