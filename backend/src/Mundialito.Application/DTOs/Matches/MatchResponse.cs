namespace Mundialito.Application.DTOs.Matches;

/// <summary>
/// Respuesta de partido para POST /matches y GET /matches/{id} (vista simple).
/// { "id", "homeTeamId", "awayTeamId", "scheduledAt", "status", "createdAt" }
/// </summary>
public sealed class MatchResponse
{
    public Guid     Id          { get; init; }
    public Guid     HomeTeamId  { get; init; }
    public Guid     AwayTeamId  { get; init; }
    public DateTime ScheduledAt { get; init; }
    public string   Status      { get; init; } = string.Empty;
    public DateTime CreatedAt   { get; init; }
}
