using Microsoft.Extensions.Logging;
using Mundialito.Application.Abstractions;
using Mundialito.Domain.Events;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Infrastructure.Persistence;

/// <summary>
/// Implementación del Unit of Work.
/// Es el ÚNICO lugar donde se llama a SaveChangesAsync (para entidades de dominio).
///
/// Flujo de CommitAsync:
///   a) Extraer entidades trackeadas y domain events (copia inmutable)
///   b) SaveChangesAsync dentro de transacción
///   c) CommitTransaction
///   d) Limpiar eventos en entidades (evita doble despacho)
///   e) Loggear cada evento con parámetros semánticos tipados (dispatch = logging estructurado)
///
/// Los logs heredan el scope de TraceId/CorrelationId creado por ObservabilityMiddleware,
/// de modo que cada evento queda vinculado al request que lo originó.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MundialitoDbContext _dbContext;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(MundialitoDbContext dbContext, ILogger<UnitOfWork> logger)
    {
        _dbContext = dbContext;
        _logger    = logger;
    }

    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        // ── a) Capturar entidades y eventos ANTES de persistir ────────────────
        // La captura se hace con .ToList() para que sea una copia inmutable;
        // si SaveChangesAsync cambia el estado del tracker, no afecta esta lista.
        var trackedEntities = _dbContext.ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = trackedEntities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // ── b + c) Persistir dentro de transacción ────────────────────────────
        await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
        await _dbContext.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // ── d) Limpiar eventos (evita doble despacho en llamadas sucesivas) ────
        foreach (var entity in trackedEntities)
            entity.ClearDomainEvents();

        // ── e) Despachar eventos = logging estructurado tipado ─────────────────
        // C# pattern matching para capturar propiedades semánticas de cada tipo.
        // Los logs heredan el scope TraceId/CorrelationId del ObservabilityMiddleware.
        foreach (var evt in domainEvents)
        {
            DispatchEvent(evt);
        }
    }

    /// <summary>
    /// Loggea el evento usando pattern matching para emitir propiedades semánticas
    /// específicas de cada tipo. Usa structured logging estricto (sin interpolación).
    /// </summary>
    private void DispatchEvent(DomainEvent evt)
    {
        if (evt is TeamCreatedEvent tce)
        {
            _logger.LogInformation(
                "DomainEvent dispatched: {EventType} TeamId={TeamId} TeamName={TeamName} OccurredAt={OccurredAt}",
                nameof(TeamCreatedEvent),
                tce.TeamId,
                tce.TeamName,
                tce.OccurredAt);
        }
        else if (evt is MatchResultRecordedEvent mre)
        {
            _logger.LogInformation(
                "DomainEvent dispatched: {EventType} MatchId={MatchId} HomeGoals={HomeGoals} AwayGoals={AwayGoals} OccurredAt={OccurredAt}",
                nameof(MatchResultRecordedEvent),
                mre.MatchId,
                mre.HomeGoals,
                mre.AwayGoals,
                mre.OccurredAt);
        }
        else
        {
            // Fallback genérico para cualquier evento futuro no tipado explícitamente.
            _logger.LogInformation(
                "DomainEvent dispatched: {EventType} OccurredAt={OccurredAt}",
                evt.GetType().Name,
                evt.OccurredAt);
        }
    }
}
