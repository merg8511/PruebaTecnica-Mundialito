using Mundialito.Domain.SeedWork;

namespace Mundialito.Domain.Events;

/// <summary>
/// Se dispara cuando un equipo es creado exitosamente.
/// Lo registra la propia entidad <see cref="Teams.Team"/> desde su factory.
/// </summary>
public sealed class TeamCreatedEvent : DomainEvent
{
    public Guid TeamId { get; }
    public string TeamName { get; }

    public TeamCreatedEvent(Guid teamId, string teamName)
    {
        TeamId = teamId;
        TeamName = teamName;
    }
}
