namespace Mundialito.Domain.SeedWork;

/// <summary>
/// Clase base para todos los Domain Events.
/// Inmutable: se captura el instante UTC en construcción.
/// </summary>
public abstract class DomainEvent
{
    protected DomainEvent()
    {
        OccurredAt = DateTime.UtcNow;
    }

    /// <summary>Momento UTC en que ocurrió el evento.</summary>
    public DateTime OccurredAt { get; }
}
