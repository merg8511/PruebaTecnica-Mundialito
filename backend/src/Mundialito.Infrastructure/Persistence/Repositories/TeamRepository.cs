using Microsoft.EntityFrameworkCore;
using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Domain.Teams;
using Mundialito.Infrastructure.Persistence;

namespace Mundialito.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core (write) para la entidad <see cref="Team"/>.
/// NO llama SaveChanges — eso es responsabilidad exclusiva de UnitOfWork.
/// </summary>
public sealed class TeamRepository : ITeamRepository
{
    private readonly MundialitoDbContext _db;

    public TeamRepository(MundialitoDbContext db) => _db = db;

    /// <inheritdoc/>
    public void Add(Team team) => _db.Teams.Add(team);

    /// <inheritdoc/>
    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Teams.FindAsync([id], ct);

    /// <inheritdoc/>
    public async Task<Team?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _db.Teams
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower(), ct);

    /// <inheritdoc/>
    public void Update(Team team) => _db.Teams.Update(team);

    /// <inheritdoc/>
    public void Remove(Team team) => _db.Teams.Remove(team);
}
