namespace Mundialito.Application.DTOs.Results;

/// <summary>
/// Entrada de gol por jugador dentro del request de resultado.
/// </summary>
public sealed class GoalByPlayerRequest
{
    /// <summary>Id del jugador que anotó.</summary>
    public Guid PlayerId { get; init; }

    /// <summary>Cantidad de goles anotados en el partido.</summary>
    public int  Goals    { get; init; }
}
