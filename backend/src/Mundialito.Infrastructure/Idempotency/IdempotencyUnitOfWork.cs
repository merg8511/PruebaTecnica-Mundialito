using Mundialito.Application.Abstractions;
using Mundialito.Infrastructure.Persistence;

namespace Mundialito.Infrastructure.Idempotency;

/// <summary>
/// Unit of Work dedicado para IdempotencyKeys.
/// Implementa IIdempotencyUnitOfWork (Application) para que la capa Api
/// no dependa directamente de un tipo concreto de Infrastructure.
/// Es el ÚNICO lugar donde se llama SaveChangesAsync para IdempotencyKey.
/// </summary>
public sealed class IdempotencyUnitOfWork : IIdempotencyUnitOfWork
{
    private readonly MundialitoDbContext _dbContext;

    public IdempotencyUnitOfWork(MundialitoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
