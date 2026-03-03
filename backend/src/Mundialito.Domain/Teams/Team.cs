using Mundialito.Domain.Events;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Domain.Teams;

/// <summary>
/// Agregado raíz que representa un equipo participante en el torneo.
/// </summary>
/// <remarks>
/// Invariantes de dominio:
/// - <see cref="Name"/> no puede ser nulo ni vacío ni solo espacios.
/// - La unicidad del nombre es responsabilidad de Application (verifica contra la DB antes de crear).
/// </remarks>
public sealed class Team : Entity
{
    /// <summary>Nombre del equipo. Único en el torneo.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Fecha/hora UTC de creación.</summary>
    public DateTime CreatedAt { get; private set; }

    // Constructor privado — solo EF Core (vía reflexión) y la factory lo usan.
    private Team() { }

    // ─── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo equipo y registra el evento <see cref="TeamCreatedEvent"/>.
    /// </summary>
    /// <param name="name">Nombre del equipo (no nulo, no vacío).</param>
    /// <returns>
    /// <see cref="Result{Team}"/> exitoso, o fallido con
    /// <see cref="DomainErrors.ValidationError"/> si el nombre no es válido.
    /// </returns>
    public static Result<Team> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Team>.Fail(
                DomainErrors.ValidationError,
                "Team name cannot be empty.");

        name = name.Trim();

        var team = new Team
        {
            Name      = name,
            CreatedAt = DateTime.UtcNow
        };
        team.SetId(Guid.NewGuid());
        team.AddDomainEvent(new TeamCreatedEvent(team.Id, team.Name));

        return Result<Team>.Ok(team);
    }

    // ─── Métodos de negocio ──────────────────────────────────────────────────

    /// <summary>
    /// Actualiza el nombre del equipo.
    /// La verificación de unicidad del nuevo nombre corre en Application.
    /// </summary>
    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Fail(
                DomainErrors.ValidationError,
                "Team name cannot be empty.");

        Name = newName.Trim();
        return Result.Ok();
    }
}
