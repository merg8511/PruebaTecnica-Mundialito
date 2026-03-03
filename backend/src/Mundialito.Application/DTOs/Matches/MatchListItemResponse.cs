namespace Mundialito.Application.DTOs.Matches;

/// <summary>
/// Ítem de lista de partidos para GET /matches (con nombres de equipos y goles).
/// { "id", "homeTeamId", "homeTeamName", "awayTeamId", "awayTeamName",
///   "scheduledAt", "status", "homeGoals"?, "awayGoals"? }
/// homeGoals/awayGoals son null cuando status = "Scheduled".
/// </summary>
public sealed class MatchListItemResponse
{
    public Guid     Id           { get; init; }
    public Guid     HomeTeamId   { get; init; }
    public string   HomeTeamName { get; init; } = string.Empty;
    public Guid     AwayTeamId   { get; init; }
    public string   AwayTeamName { get; init; } = string.Empty;
    public DateTime ScheduledAt  { get; init; }
    public string   Status       { get; init; } = string.Empty;
    public int?     HomeGoals    { get; init; }
    public int?     AwayGoals    { get; init; }
}
