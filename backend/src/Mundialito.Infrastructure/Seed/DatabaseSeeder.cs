using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mundialito.Domain.Matches;
using Mundialito.Domain.Players;
using Mundialito.Domain.Results;
using Mundialito.Domain.Teams;
using Mundialito.Infrastructure.Persistence;

namespace Mundialito.Infrastructure.Seed;

/// <summary>
/// Seed inicial del torneo: 4 equipos, 5 jugadores/equipo, 6 partidos, 3 con resultado.
/// Se ejecuta UNA SOLA VEZ (solo si la tabla Teams está vacía).
///
/// Reglas DDD estrictas:
/// - TODAS las entidades se crean con sus factories (Team.Create, Player.Create, etc.).
/// - PROHIBIDO instanciar directamente ni usar setters públicos.
/// - La coherencia goles-por-jugador == homeGoals/awayGoals se valida antes de insertar.
///
/// Tabla de posiciones resultante:
///   Team A: Played=2, W=1, D=1, L=0 → 4 pts | GF=3 GA=2 GD=+1
///   Team B: Played=2, W=1, D=0, L=1 → 3 pts | GF=4 GA=2 GD=+2
///   Team C: Played=1, W=0, D=1, L=0 → 1 pt  | GF=1 GA=1 GD=0
///   Team D: Played=1, W=0, D=0, L=1 → 0 pts | GF=0 GA=3 GD=-3
///
/// Goleadores (scorers):
///   Team A Player 1: 2 (match A-B) + 1 (match A-C) = 3 total
///   Team B Player 1: 1 (match A-B)
///   Team C Player 1: 1 (match A-C)
///   Team B Player 2: 2 (match B-D)
///   Team B Player 3: 1 (match B-D)
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly MundialitoDbContext     _db;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(MundialitoDbContext db, ILogger<DatabaseSeeder> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // ── Guard: solo si la BD está vacía ──────────────────────────────────
        if (await _db.Teams.AnyAsync(ct))
        {
            _logger.LogInformation("Seed skipped — database already contains data.");
            return;
        }

        _logger.LogInformation("Starting database seed (4 teams / 5 players / 6 matches / 3 results)...");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // ── 1. Teams ──────────────────────────────────────────────────────────
        var teamNames = new[] { "Team A", "Team B", "Team C", "Team D" };
        var teams     = new List<Team>(4);

        foreach (var name in teamNames)
        {
            var r = Team.Create(name);
            AssertSuccess(r.IsSuccess, $"Team.Create(\"{name}\")");

            var team = r.Value
                ?? throw new InvalidOperationException(
                    $"Team.Create(\"{name}\") returned IsSuccess=true but Value=null. " +
                    "This is a programming error in the domain factory.");

            teams.Add(team);
            _db.Teams.Add(team);
        }

        // Persist teams first so we have their IDs for players/matches
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded {Count} teams.", teams.Count);

        var (teamA, teamB, teamC, teamD) = (teams[0], teams[1], teams[2], teams[3]);

        // ── 2. Players (5 per team) ───────────────────────────────────────────
        var playersByTeam = new Dictionary<Guid, List<Player>>();

        foreach (var team in teams)
        {
            var list = new List<Player>(5);
            for (var i = 1; i <= 5; i++)
            {
                var playerName = $"{team.Name} Player {i}";
                var pr = Player.Create(team.Id, playerName, number: i);
                AssertSuccess(pr.IsSuccess, $"Player.Create(teamId={team.Id}, \"{playerName}\", number={i})");

                var player = pr.Value
                    ?? throw new InvalidOperationException(
                        $"Player.Create(\"{playerName}\") returned IsSuccess=true but Value=null. " +
                        "This is a programming error in the domain factory.");

                list.Add(player);
                _db.Players.Add(player);
            }
            playersByTeam[team.Id] = list;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded 20 players (5 per team).");

        // ── 3. Matches (6 — full round-robin for 4 teams) ────────────────────
        // Scheduled dates: 2026-04-01 through 2026-04-06 at 18:00 UTC
        var baseDate = new DateTime(2026, 4, 1, 18, 0, 0, DateTimeKind.Utc);

        // Match 0: Team A vs Team B → Played  (2-1)
        // Match 1: Team A vs Team C → Played  (1-1)
        // Match 2: Team B vs Team D → Played  (3-0)
        // Match 3: Team A vs Team D → Scheduled
        // Match 4: Team B vs Team C → Scheduled
        // Match 5: Team C vs Team D → Scheduled
        var matchDefs = new (Team Home, Team Away, DateTime ScheduledAt)[]
        {
            (teamA, teamB, baseDate.AddDays(0)),
            (teamA, teamC, baseDate.AddDays(1)),
            (teamB, teamD, baseDate.AddDays(2)),
            (teamA, teamD, baseDate.AddDays(3)),
            (teamB, teamC, baseDate.AddDays(4)),
            (teamC, teamD, baseDate.AddDays(5)),
        };

        var createdMatches = new List<Match>(6);

        foreach (var (home, away, scheduledAt) in matchDefs)
        {
            var context = $"Match.Create(homeTeamId={home.Id} \"{home.Name}\" vs awayTeamId={away.Id} \"{away.Name}\", scheduledAt={scheduledAt:u})";
            var mr = Match.Create(home.Id, away.Id, scheduledAt);
            AssertSuccess(mr.IsSuccess, context);

            var match = mr.Value
                ?? throw new InvalidOperationException(
                    $"{context} returned IsSuccess=true but Value=null. " +
                    "This is a programming error in the domain factory.");

            createdMatches.Add(match);
            _db.Matches.Add(match);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded 6 matches.");

        // ── 4. Results + Goals for the 3 played matches ───────────────────────
        var playedMatchDefs = new[]
        {
            new
            {
                Match     = createdMatches[0],
                HomeTeam  = teamA,
                AwayTeam  = teamB,
                HomeGoals = 2,
                AwayGoals = 1,
                // Goals must sum to HomeGoals for home players and AwayGoals for away players.
                Goals = new[]
                {
                    (PlayerIndex: 0, Team: teamA, Goals: 2),  // Team A Player 1 → 2 goals
                    (PlayerIndex: 0, Team: teamB, Goals: 1),  // Team B Player 1 → 1 goal
                }
            },
            new
            {
                Match     = createdMatches[1],
                HomeTeam  = teamA,
                AwayTeam  = teamC,
                HomeGoals = 1,
                AwayGoals = 1,
                Goals = new[]
                {
                    (PlayerIndex: 0, Team: teamA, Goals: 1),  // Team A Player 1 → 1 goal
                    (PlayerIndex: 0, Team: teamC, Goals: 1),  // Team C Player 1 → 1 goal
                }
            },
            new
            {
                Match     = createdMatches[2],
                HomeTeam  = teamB,
                AwayTeam  = teamD,
                HomeGoals = 3,
                AwayGoals = 0,
                Goals = new[]
                {
                    (PlayerIndex: 1, Team: teamB, Goals: 2),  // Team B Player 2 → 2 goals
                    (PlayerIndex: 2, Team: teamB, Goals: 1),  // Team B Player 3 → 1 goal
                }
            },
        };

        foreach (var def in playedMatchDefs)
        {
            // a) Transition match to Played
            var playResult = def.Match.MarkAsPlayed();
            AssertSuccess(playResult.IsSuccess, $"Match.MarkAsPlayed(matchId={def.Match.Id})");
            _db.Matches.Update(def.Match);

            // b) Create MatchResult
            var resultCtx    = $"MatchResult.Create(matchId={def.Match.Id}, homeGoals={def.HomeGoals}, awayGoals={def.AwayGoals})";
            var resultCreate = MatchResult.Create(def.Match.Id, def.HomeGoals, def.AwayGoals);
            AssertSuccess(resultCreate.IsSuccess, resultCtx);

            var matchResult = resultCreate.Value
                ?? throw new InvalidOperationException(
                    $"{resultCtx} returned IsSuccess=true but Value=null. " +
                    "This is a programming error in the domain factory.");

            // Register the domain event (DDD contract: raised on MatchResult entity)
            matchResult.RegisterResultRecordedEvent(def.Match.Id, def.HomeGoals, def.AwayGoals);
            _db.MatchResults.Add(matchResult);

            // c) Create MatchGoals — each entry attributed to the correct team
            var homeSum = 0;
            var awaySum = 0;

            foreach (var (playerIndex, team, goals) in def.Goals)
            {
                var player    = playersByTeam[team.Id][playerIndex];
                var goalCtx   = $"MatchGoal.Create(matchId={def.Match.Id}, playerId={player.Id} \"{player.FullName}\", teamId={team.Id}, goals={goals})";
                var goalCreate = MatchGoal.Create(def.Match.Id, player.Id, team.Id, goals);
                AssertSuccess(goalCreate.IsSuccess, goalCtx);

                var goal = goalCreate.Value
                    ?? throw new InvalidOperationException(
                        $"{goalCtx} returned IsSuccess=true but Value=null. " +
                        "This is a programming error in the domain factory.");

                _db.MatchGoals.Add(goal);

                if (team.Id == def.HomeTeam.Id) homeSum += goals;
                else awaySum += goals;
            }

            // Validate coherence inline: sum of player goals must match declared score
            if (homeSum != def.HomeGoals || awaySum != def.AwayGoals)
            {
                _logger.LogError(
                    "Seed coherence error in match {MatchId}: " +
                    "homePlayerGoals={HomeSum} vs declared HomeGoals={HomeGoals}, " +
                    "awayPlayerGoals={AwaySum} vs declared AwayGoals={AwayGoals}. Rolling back.",
                    def.Match.Id, homeSum, def.HomeGoals, awaySum, def.AwayGoals);
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException(
                    $"Seed coherence error in match {def.Match.Id}: " +
                    $"homeSum={homeSum}≠{def.HomeGoals} or awaySum={awaySum}≠{def.AwayGoals}. " +
                    "This is a programming error in the seed data.");
            }
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation(
            "Seed applied successfully. Teams=4 Players=20 Matches=6 (Played=3 Scheduled=3) Results=3.");
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> with a clear diagnostic message
    /// when a domain factory returns failure during seeding.
    /// Using exceptions here is correct: seed failures are programming-time errors,
    /// not business-time errors.
    /// </summary>
    private static void AssertSuccess(bool isSuccess, string factoryContext)
    {
        if (!isSuccess)
            throw new InvalidOperationException(
                $"Seed factory returned failure at: [{factoryContext}]. " +
                "This indicates a programming error in the seed data — domain invariants not satisfied.");
    }
}
