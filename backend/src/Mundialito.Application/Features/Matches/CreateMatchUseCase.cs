using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Application.DTOs.Matches;
using Mundialito.Domain.Matches;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Features.Matches;

/// <summary>
/// Caso de uso: Crear un partido.
/// Validaciones:
///   - homeTeamId != awayTeamId → VALIDATION_ERROR (400)
///   - ambos equipos existen → TEAM_NOT_FOUND (404)
/// </summary>
public sealed class CreateMatchUseCase
{
    private readonly IMatchRepository _matchRepository;
    private readonly ITeamRepository  _teamRepository;
    private readonly IUnitOfWork      _unitOfWork;

    public CreateMatchUseCase(
        IMatchRepository matchRepository,
        ITeamRepository  teamRepository,
        IUnitOfWork      unitOfWork)
    {
        _matchRepository = matchRepository;
        _teamRepository  = teamRepository;
        _unitOfWork      = unitOfWork;
    }

    public async Task<Result<MatchResponse>> ExecuteAsync(
        CreateMatchRequest request,
        CancellationToken  ct = default)
    {
        // 1. Mismos equipos → VALIDATION_ERROR (400).
        if (request.HomeTeamId == request.AwayTeamId)
            return Result<MatchResponse>.Fail(
                DomainErrors.ValidationError,
                "A team cannot play against itself.");

        // 2. Equipo local debe existir.
        var homeTeam = await _teamRepository.GetByIdAsync(request.HomeTeamId, ct);
        if (homeTeam is null)
            return Result<MatchResponse>.Fail(
                DomainErrors.TeamNotFound,
                $"Home team '{request.HomeTeamId}' was not found.");

        // 3. Equipo visitante debe existir.
        var awayTeam = await _teamRepository.GetByIdAsync(request.AwayTeamId, ct);
        if (awayTeam is null)
            return Result<MatchResponse>.Fail(
                DomainErrors.TeamNotFound,
                $"Away team '{request.AwayTeamId}' was not found.");

        // 4. Crear entidad vía factory de dominio.
        var createResult = Match.Create(request.HomeTeamId, request.AwayTeamId, request.ScheduledAt);
        if (createResult.IsFailure)
            return Result<MatchResponse>.Fail(createResult.ErrorCode!, createResult.ErrorMessage!);

        var match = createResult.Value;

        // 5. Persistir y commitear.
        _matchRepository.Add(match);
        await _unitOfWork.CommitAsync(ct);

        return Result<MatchResponse>.Ok(new MatchResponse
        {
            Id          = match.Id,
            HomeTeamId  = match.HomeTeamId,
            AwayTeamId  = match.AwayTeamId,
            ScheduledAt = match.ScheduledAt,
            Status      = match.Status.ToString(),
            CreatedAt   = match.CreatedAt
        });
    }
}
