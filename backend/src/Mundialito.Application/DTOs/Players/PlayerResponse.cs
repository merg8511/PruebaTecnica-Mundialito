namespace Mundialito.Application.DTOs.Players;

/// <summary>
/// Respuesta de jugador 
/// { "id", "teamId", "fullName", "number", "createdAt" }
/// </summary>
public sealed class PlayerResponse
{
    public Guid     Id        { get; init; }
    public Guid     TeamId    { get; init; }
    public string   FullName  { get; init; } = string.Empty;
    public int?     Number    { get; init; }
    public DateTime CreatedAt { get; init; }
}
