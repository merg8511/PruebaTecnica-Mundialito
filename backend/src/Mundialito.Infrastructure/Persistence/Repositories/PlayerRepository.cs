using Microsoft.EntityFrameworkCore;
using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Domain.Players;
using Mundialito.Infrastructure.Persistence;

namespace Mundialito.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core (write) para la entidad <see cref="Player"/>.
/// NO llama SaveChanges — eso es responsabilidad exclusiva de UnitOfWork.
/// </summary>
public sealed class PlayerRepository : IPlayerRepository
{
    private readonly MundialitoDbContext _db;

    public PlayerRepository(MundialitoDbContext db) => _db = db;

    /// <inheritdoc/>
    public void Add(Player player) => _db.Players.Add(player);

    /// <inheritdoc/>
    public async Task<Player?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Players.FindAsync([id], ct);

    /// <inheritdoc/>
    public void Update(Player player) => _db.Players.Update(player);

    /// <inheritdoc/>
    public void Remove(Player player) => _db.Players.Remove(player);
}
