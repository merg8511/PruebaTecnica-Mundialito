namespace Mundialito.Application.DTOs.Results;

/// <summary>
/// Entrada del desglose de goles en la respuesta del resultado.
/// </summary>
public sealed class GoalByPlayerResponse
{
    public Guid PlayerId { get; init; }
    public Guid TeamId   { get; init; }
    public int  Goals    { get; init; }
}
