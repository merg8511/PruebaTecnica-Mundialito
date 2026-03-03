namespace Mundialito.Application.DTOs.Matches;

/// <summary>
/// Detalle de partido para GET /matches/{id}.
/// Incluye información de equipos y resultado si existe.
/// </summary>
public sealed class MatchDetailResponse
{
    public Guid     Id           { get; init; }
    public Guid     HomeTeamId   { get; init; }
    public string   HomeTeamName { get; init; } = string.Empty;
    public Guid     AwayTeamId   { get; init; }
    public string   AwayTeamName { get; init; } = string.Empty;
    public DateTime ScheduledAt  { get; init; }
    public string   Status       { get; init; } = string.Empty;
    public DateTime CreatedAt    { get; init; }
    public int?     HomeGoals    { get; init; }
    public int?     AwayGoals    { get; init; }
}
