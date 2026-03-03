namespace Mundialito.Application.DTOs.Players;

/// <summary>Request de actualización de jugador (PUT /players/{id}).</summary>
public sealed class UpdatePlayerRequest
{
    /// <summary>Nombre completo del jugador. Requerido.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Número de dorsal. Opcional.</summary>
    public int? Number { get; init; }
}
