namespace Mundialito.Domain.SeedWork;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// Gestiona el Id (Guid) y la colección interna de Domain Events.
/// </summary>
public abstract class Entity
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <summary>Identificador único de la entidad.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Colección de eventos de dominio pendientes de despacho.
    /// Solo lectura para el exterior; se gestiona via <see cref="AddDomainEvent"/>.
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Constructor protegido para uso exclusivo de las subclases (y EF Core).</summary>
    protected Entity() { }

    /// <summary>Establece el Id al momento de creación.</summary>
    protected void SetId(Guid id) => Id = id;

    /// <summary>Registra un evento de dominio en la entidad.</summary>
    protected void AddDomainEvent(DomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <summary>Elimina todos los eventos de dominio (tras despacho correcto).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
