using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Domain.Matches;
using Mundialito.Infrastructure.Persistence;

namespace Mundialito.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core (write) para la entidad <see cref="Match"/>.
/// NO llama SaveChanges — eso es responsabilidad exclusiva de UnitOfWork.
/// </summary>
public sealed class MatchRepository : IMatchRepository
{
    private readonly MundialitoDbContext _db;

    public MatchRepository(MundialitoDbContext db) => _db = db;

    /// <inheritdoc/>
    public void Add(Match match) => _db.Matches.Add(match);

    /// <inheritdoc/>
    public async Task<Match?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Matches.FindAsync([id], ct);

    /// <inheritdoc/>
    public void Update(Match match) => _db.Matches.Update(match);
}
