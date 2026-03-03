namespace Mundialito.Application.DTOs.Teams;

/// <summary>Request de actualización de equipo (PUT /teams/{id}).</summary>
public sealed class UpdateTeamRequest
{
    /// <summary>Nuevo nombre del equipo. Requerido.</summary>
    public string Name { get; init; } = string.Empty;
}
