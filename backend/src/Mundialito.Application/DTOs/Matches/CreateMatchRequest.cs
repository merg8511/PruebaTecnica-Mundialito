namespace Mundialito.Application.DTOs.Matches;

/// <summary>Request de creación de partido (POST /matches).</summary>
public sealed class CreateMatchRequest
{
    /// <summary>Id del equipo local. Requerido.</summary>
    public Guid     HomeTeamId  { get; init; }

    /// <summary>Id del equipo visitante. Requerido.</summary>
    public Guid     AwayTeamId  { get; init; }

    /// <summary>Fecha/hora programada del partido (ISO-8601). Requerida.</summary>
    public DateTime ScheduledAt { get; init; }
}
