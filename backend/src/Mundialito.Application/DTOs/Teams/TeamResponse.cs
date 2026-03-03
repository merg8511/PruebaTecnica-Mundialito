namespace Mundialito.Application.DTOs.Teams;

/// <summary>
/// Respuesta de equipo según BLUEPRINT:
/// { "id": "uuid", "name": "Team A", "createdAt": "ISO-8601" }
/// </summary>
public sealed class TeamResponse
{
    public Guid     Id        { get; init; }
    public string   Name      { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
