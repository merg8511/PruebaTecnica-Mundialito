namespace Mundialito.Application.DTOs.Teams;

/// <summary>Request de creación de equipo (POST /teams).</summary>
public sealed class CreateTeamRequest
{
    /// <summary>Nombre del equipo. Requerido.</summary>
    public string Name { get; init; } = string.Empty;
}
