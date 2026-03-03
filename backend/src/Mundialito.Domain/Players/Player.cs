using Mundialito.Domain.SeedWork;

namespace Mundialito.Domain.Players;

/// <summary>
/// Jugador perteneciente a un <see cref="Teams.Team"/>.
/// </summary>
/// <remarks>
/// Invariantes de dominio:
/// - <see cref="FullName"/> es obligatorio (no nulo ni vacío).
/// - <see cref="Number"/> es opcional; si se proporciona debe ser &gt; 0.
/// - La unicidad del número de dorsal dentro del equipo se valida en Application.
/// </remarks>
public sealed class Player : Entity
{
    /// <summary>Identificador del equipo al que pertenece.</summary>
    public Guid TeamId { get; private set; }

    /// <summary>Nombre completo del jugador.</summary>
    public string FullName { get; private set; } = default!;

    /// <summary>Número de dorsal (opcional).</summary>
    public int? Number { get; private set; }

    /// <summary>Fecha/hora UTC de creación.</summary>
    public DateTime CreatedAt { get; private set; }

    // Constructor privado para EF Core y la factory.
    private Player() { }

    // ─── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo jugador asociado a un equipo.
    /// </summary>
    /// <param name="teamId">Id del equipo al que pertenece.</param>
    /// <param name="fullName">Nombre completo (no nulo, no vacío).</param>
    /// <param name="number">Número de dorsal opcional. Si se proporciona debe ser &gt; 0.</param>
    public static Result<Player> Create(Guid teamId, string fullName, int? number = null)
    {
        if (teamId == Guid.Empty)
            return Result<Player>.Fail(
                DomainErrors.ValidationError,
                "TeamId is required.");

        if (string.IsNullOrWhiteSpace(fullName))
            return Result<Player>.Fail(
                DomainErrors.ValidationError,
                "Player full name cannot be empty.");

        if (number.HasValue && number.Value <= 0)
            return Result<Player>.Fail(
                DomainErrors.ValidationError,
                "Player number must be greater than zero.");

        var player = new Player
        {
            TeamId = teamId,
            FullName = fullName.Trim(),
            Number = number,
            CreatedAt = DateTime.UtcNow
        };
        player.SetId(Guid.NewGuid());

        return Result<Player>.Ok(player);
    }

    // ─── Métodos de negocio ──────────────────────────────────────────────────

    /// <summary>
    /// Actualiza los datos editables del jugador.
    /// La verificación de dorsal duplicado dentro del equipo corre en Application.
    /// </summary>
    public Result Update(string fullName, int? number)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Fail(
                DomainErrors.ValidationError,
                "Player full name cannot be empty.");

        if (number.HasValue && number.Value <= 0)
            return Result.Fail(
                DomainErrors.ValidationError,
                "Player number must be greater than zero.");

        FullName = fullName.Trim();
        Number = number;
        return Result.Ok();
    }
}
