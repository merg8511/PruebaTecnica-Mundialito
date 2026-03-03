namespace Mundialito.Application.DTOs.Players;

/// <summary>Request de creación de jugador (POST /teams/{teamId}/players).</summary>
public sealed class CreatePlayerRequest
{
    /// <summary>Nombre completo del jugador. Requerido.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Número de dorsal. Opcional.</summary>
    public int? Number { get; init; }
}
