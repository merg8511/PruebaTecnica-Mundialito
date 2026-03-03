using Mundialito.Application.Abstractions;
using Mundialito.Infrastructure.Persistence;

namespace Mundialito.Infrastructure.Idempotency;

/// <summary>
/// Repositorio de escritura (EF Core) para registros de idempotencia.
/// CQRS: SOLO write. El commit lo realiza el IdempotencyUnitOfWork.
/// No llama SaveChangesAsync directamente.
/// </summary>
public sealed class IdempotencyRepository : IIdempotencyRepository
{
    private readonly MundialitoDbContext _dbContext;

    public IdempotencyRepository(MundialitoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public Task SaveAsync(
        string idempotencyKey,
        string requestHash,
        int    responseStatusCode,
        string responseBody,
        CancellationToken ct = default)
    {
        var entity = new IdempotencyKey
        {
            Id                  = Guid.NewGuid(),
            IdempotencyKeyValue = idempotencyKey,
            RequestHash         = requestHash,
            ResponseStatusCode  = responseStatusCode,
            ResponseBody        = responseBody,
            CreatedAt           = DateTime.UtcNow
        };

        _dbContext.IdempotencyKeys.Add(entity);

        // No llamamos SaveChangesAsync aquí — el UoW dedicado lo hará.
        return Task.CompletedTask;
    }
}
