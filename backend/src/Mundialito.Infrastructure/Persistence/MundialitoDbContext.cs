using Microsoft.EntityFrameworkCore;
using Mundialito.Domain.Events;
using Mundialito.Domain.Matches;
using Mundialito.Domain.Players;
using Mundialito.Domain.Results;
using Mundialito.Domain.SeedWork;
using Mundialito.Domain.Teams;

namespace Mundialito.Infrastructure.Persistence;

/// <summary>
/// DbContext principal de la aplicación.
/// SOLO se usa para escritura (Commands / EF Core).
/// Las lecturas (Queries) van por Dapper (Sprint 4).
/// </summary>
public sealed class MundialitoDbContext : DbContext
{
    public MundialitoDbContext(DbContextOptions<MundialitoDbContext> options)
        : base(options) { }

    // ── DbSets ───────────────────────────────────────────────────────────────

    public DbSet<Team>          Teams          { get; set; } = default!;
    public DbSet<Player>        Players        { get; set; } = default!;
    public DbSet<Match>         Matches        { get; set; } = default!;
    public DbSet<MatchResult>   MatchResults   { get; set; } = default!;
    public DbSet<MatchGoal>     MatchGoals     { get; set; } = default!;
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; } = default!;

    // ── Fluent API ───────────────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>(); // Ignora la clase base de eventos de dominio
        modelBuilder.Ignore<TeamCreatedEvent>();
        modelBuilder.Ignore<MatchResultRecordedEvent>();

        base.OnModelCreating(modelBuilder);

        // ── Teams ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Team>(e =>
        {
            e.ToTable("Teams");
            e.HasKey(t => t.Id);

            e.Property(t => t.Name)
             .IsRequired()
             .HasMaxLength(200);

            e.Property(t => t.CreatedAt)
             .IsRequired();

            // Unique index — garantiza unicidad del nombre de equipo
            e.HasIndex(t => t.Name)
             .IsUnique()
             .HasDatabaseName("IX_Teams_Name");
        });

        // ── Players ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Player>(e =>
        {
            e.ToTable("Players");
            e.HasKey(p => p.Id);

            e.Property(p => p.FullName)
             .IsRequired()
             .HasMaxLength(300);

            e.Property(p => p.Number)
             .IsRequired(false);

            e.Property(p => p.CreatedAt)
             .IsRequired();

            // FK -> Teams (Restrict para evitar "multiple cascade paths")
            e.HasOne<Team>()
             .WithMany()
             .HasForeignKey(p => p.TeamId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Matches ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Match>(e =>
        {
            e.ToTable("Matches");
            e.HasKey(m => m.Id);

            e.Property(m => m.ScheduledAt)
             .IsRequired();

            e.Property(m => m.CreatedAt)
             .IsRequired();

            e.Property(m => m.Status)
             .IsRequired()
             .HasConversion<string>()
             .HasMaxLength(50);

            // FK HomeTeamId -> Teams (Restrict — evita múltiples cascade paths)
            e.HasOne<Team>()
             .WithMany()
             .HasForeignKey(m => m.HomeTeamId)
             .OnDelete(DeleteBehavior.Restrict);

            // FK AwayTeamId -> Teams (Restrict — evita múltiples cascade paths)
            e.HasOne<Team>()
             .WithMany()
             .HasForeignKey(m => m.AwayTeamId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MatchResults ─────────────────────────────────────────────────────
        modelBuilder.Entity<MatchResult>(e =>
        {
            e.ToTable("MatchResults");
            e.HasKey(r => r.Id);

            e.Property(r => r.HomeGoals).IsRequired();
            e.Property(r => r.AwayGoals).IsRequired();
            e.Property(r => r.RecordedAt).IsRequired();

            // Unique index en MatchId — garantiza exactamente 1 resultado por partido
            e.HasIndex(r => r.MatchId)
             .IsUnique()
             .HasDatabaseName("IX_MatchResults_MatchId");

            // FK MatchId -> Matches (Restrict — evita múltiples cascade paths)
            e.HasOne<Match>()
             .WithMany()
             .HasForeignKey(r => r.MatchId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MatchGoals ───────────────────────────────────────────────────────
        modelBuilder.Entity<MatchGoal>(e =>
        {
            e.ToTable("MatchGoals");
            e.HasKey(g => g.Id);

            e.Property(g => g.Goals).IsRequired();
            e.Property(g => g.CreatedAt).IsRequired();

            // FK MatchId -> Matches (Restrict)
            e.HasOne<Match>()
             .WithMany()
             .HasForeignKey(g => g.MatchId)
             .OnDelete(DeleteBehavior.Restrict);

            // FK PlayerId -> Players (Restrict)
            e.HasOne<Player>()
             .WithMany()
             .HasForeignKey(g => g.PlayerId)
             .OnDelete(DeleteBehavior.Restrict);

            // FK TeamId -> Teams (Restrict)
            e.HasOne<Team>()
             .WithMany()
             .HasForeignKey(g => g.TeamId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── IdempotencyKeys ──────────────────────────────────────────────────
        modelBuilder.Entity<IdempotencyKey>(e =>
        {
            e.ToTable("IdempotencyKeys");
            e.HasKey(k => k.Id);

            e.Property(k => k.IdempotencyKeyValue)
             .IsRequired()
             .HasColumnName("IdempotencyKey")
             .HasMaxLength(500);

            e.Property(k => k.RequestHash)
             .IsRequired()
             .HasMaxLength(500);

            e.Property(k => k.ResponseStatusCode)
             .IsRequired();

            e.Property(k => k.ResponseBody)
             .IsRequired();

            e.Property(k => k.CreatedAt)
             .IsRequired();

            // Unique index — la key de idempotencia debe ser única
            e.HasIndex(k => k.IdempotencyKeyValue)
             .IsUnique()
             .HasDatabaseName("IX_IdempotencyKeys_IdempotencyKey");
        });
    }
}
