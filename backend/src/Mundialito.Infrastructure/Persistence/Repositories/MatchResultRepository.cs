using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Domain.Results;
using Mundialito.Infrastructure.Persistence;

namespace Mundialito.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core (write) para <see cref="MatchResult"/> y <see cref="MatchGoal"/>.
/// NO llama SaveChanges — eso es responsabilidad exclusiva de UnitOfWork.
/// </summary>
public sealed class MatchResultRepository : IMatchResultRepository
{
    private readonly MundialitoDbContext _db;

    public MatchResultRepository(MundialitoDbContext db) => _db = db;

    /// <inheritdoc/>
    public void AddResult(MatchResult result) => _db.MatchResults.Add(result);

    /// <inheritdoc/>
    public void AddGoal(MatchGoal goal) => _db.MatchGoals.Add(goal);
}
