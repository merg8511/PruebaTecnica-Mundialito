using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Application.DTOs.Results;
using Mundialito.Domain.Results;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Features.Matches;

/// <summary>
/// Caso de uso: Registrar el resultado de un partido (POST /matches/{id}/results).
///
/// Validaciones obligatorias:
///   1. match existe                              → MATCH_NOT_FOUND (404)
///   2. match no está en estado Played            → MATCH_ALREADY_PLAYED (409)
///   3. cada player en goalsByPlayer existe       → PLAYER_NOT_FOUND (404)
///   4. player pertenece a homeTeam o awayTeam    → PLAYER_NOT_IN_MATCH (400)
///   5. suma goles home cuadra con homeGoals      → MATCH_RESULT_INCONSISTENT (400)
///   6. suma goles away cuadra con awayGoals      → MATCH_RESULT_INCONSISTENT (400)
///
/// Domain event: <see cref="Domain.Events.MatchResultRecordedEvent"/> se registra
/// en <see cref="MatchResult"/> y lo despacha Infrastructure al commitear el UoW.
/// </summary>
public sealed class RecordMatchResultUseCase
{
    private readonly IMatchRepository       _matchRepository;
    private readonly IPlayerRepository      _playerRepository;
    private readonly IMatchResultRepository _matchResultRepository;
    private readonly IUnitOfWork            _unitOfWork;

    public RecordMatchResultUseCase(
        IMatchRepository       matchRepository,
        IPlayerRepository      playerRepository,
        IMatchResultRepository matchResultRepository,
        IUnitOfWork            unitOfWork)
    {
        _matchRepository       = matchRepository;
        _playerRepository      = playerRepository;
        _matchResultRepository = matchResultRepository;
        _unitOfWork            = unitOfWork;
    }

    public async Task<Result<MatchResultResponse>> ExecuteAsync(
        Guid                     matchId,
        RecordMatchResultRequest request,
        CancellationToken        ct = default)
    {
        // ── 1. Match debe existir ────────────────────────────────────────────
        var match = await _matchRepository.GetByIdAsync(matchId, ct);
        if (match is null)
            return Result<MatchResultResponse>.Fail(
                DomainErrors.MatchNotFound,
                $"Match '{matchId}' was not found.");

        // ── 2. Transición a Played (falla si ya está Played) ─────────────────
        var playedResult = match.MarkAsPlayed();
        if (playedResult.IsFailure)
            return Result<MatchResultResponse>.Fail(
                playedResult.ErrorCode!,
                playedResult.ErrorMessage!);

        // ── 3 & 4. Validar jugadores + acumular goles por equipo ─────────────
        var goalEntries      = request.GoalsByPlayer ?? [];
        var goalResponseList = new List<GoalByPlayerResponse>(goalEntries.Count);
        var homePlayerGoals  = 0;
        var awayPlayerGoals  = 0;

        foreach (var entry in goalEntries)
        {
            // 3. Jugador debe existir.
            var player = await _playerRepository.GetByIdAsync(entry.PlayerId, ct);
            if (player is null)
                return Result<MatchResultResponse>.Fail(
                    DomainErrors.PlayerNotFound,
                    $"Player '{entry.PlayerId}' was not found.");

            // 4. Jugador debe pertenecer a uno de los dos equipos del partido.
            if (player.TeamId != match.HomeTeamId && player.TeamId != match.AwayTeamId)
                return Result<MatchResultResponse>.Fail(
                    DomainErrors.PlayerNotInMatch,
                    $"Player '{entry.PlayerId}' does not belong to either team in this match.");

            // Acumular goles por equipo.
            if (player.TeamId == match.HomeTeamId)
                homePlayerGoals += entry.Goals;
            else
                awayPlayerGoals += entry.Goals;

            goalResponseList.Add(new GoalByPlayerResponse
            {
                PlayerId = player.Id,
                TeamId   = player.TeamId,
                Goals    = entry.Goals
            });
        }

        // ── 5. Consistencia goles local ───────────────────────────────────────
        if (homePlayerGoals != request.HomeGoals)
            return Result<MatchResultResponse>.Fail(
                DomainErrors.MatchResultInconsistent,
                $"Sum of home player goals ({homePlayerGoals}) does not match homeGoals ({request.HomeGoals}).");

        // ── 6. Consistencia goles visitante ───────────────────────────────────
        if (awayPlayerGoals != request.AwayGoals)
            return Result<MatchResultResponse>.Fail(
                DomainErrors.MatchResultInconsistent,
                $"Sum of away player goals ({awayPlayerGoals}) does not match awayGoals ({request.AwayGoals}).");

        // ── 7. Crear entidad MatchResult via dominio ──────────────────────────
        var resultCreate = MatchResult.Create(matchId, request.HomeGoals, request.AwayGoals);
        if (resultCreate.IsFailure)
            return Result<MatchResultResponse>.Fail(
                resultCreate.ErrorCode!,
                resultCreate.ErrorMessage!);

        var matchResult = resultCreate.Value;

        // Registrar Domain Event en MatchResult.
        matchResult.RegisterResultRecordedEvent(matchId, request.HomeGoals, request.AwayGoals);

        // ── 8. Crear entidades MatchGoal via dominio ───────────────────────────
        foreach (var resp in goalResponseList)
        {
            var goalCreate = MatchGoal.Create(matchId, resp.PlayerId, resp.TeamId, resp.Goals);
            if (goalCreate.IsFailure)
                return Result<MatchResultResponse>.Fail(
                    goalCreate.ErrorCode!,
                    goalCreate.ErrorMessage!);

            _matchResultRepository.AddGoal(goalCreate.Value);
        }

        // ── 9. Persistir todo en UoW ──────────────────────────────────────────
        _matchRepository.Update(match);               // Status = Played
        _matchResultRepository.AddResult(matchResult); // MatchResult
        // Goals ya están en contexto vía AddGoal() arriba.

        await _unitOfWork.CommitAsync(ct);

        return Result<MatchResultResponse>.Ok(new MatchResultResponse
        {
            MatchId       = matchId,
            HomeGoals     = request.HomeGoals,
            AwayGoals     = request.AwayGoals,
            RecordedAt    = matchResult.RecordedAt,
            GoalsByPlayer = goalResponseList
        });
    }
}
