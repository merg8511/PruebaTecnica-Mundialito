using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Application.DTOs.Players;
using Mundialito.Domain.Players;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Features.Players;

/// <summary>
/// Caso de uso: Crear un jugador en un equipo.
/// Validaciones:
///   - equipo debe existir → TEAM_NOT_FOUND (404)
///   - fullName requerido → VALIDATION_ERROR (400)
/// </summary>
public sealed class CreatePlayerUseCase
{
    private readonly ITeamRepository   _teamRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IUnitOfWork       _unitOfWork;

    public CreatePlayerUseCase(
        ITeamRepository   teamRepository,
        IPlayerRepository playerRepository,
        IUnitOfWork       unitOfWork)
    {
        _teamRepository   = teamRepository;
        _playerRepository = playerRepository;
        _unitOfWork       = unitOfWork;
    }

    public async Task<Result<PlayerResponse>> ExecuteAsync(
        Guid                teamId,
        CreatePlayerRequest request,
        CancellationToken   ct = default)
    {
        // 1. El equipo debe existir.
        var team = await _teamRepository.GetByIdAsync(teamId, ct);
        if (team is null)
            return Result<PlayerResponse>.Fail(
                DomainErrors.TeamNotFound,
                $"Team '{teamId}' was not found.");

        // 2. fullName requerido.
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Result<PlayerResponse>.Fail(
                DomainErrors.ValidationError,
                "Player full name is required.");

        // 3. Crear entidad vía factory de dominio.
        var createResult = Player.Create(teamId, request.FullName, request.Number);
        if (createResult.IsFailure)
            return Result<PlayerResponse>.Fail(createResult.ErrorCode!, createResult.ErrorMessage!);

        var player = createResult.Value;

        // 4. Persistir y commitear.
        _playerRepository.Add(player);
        await _unitOfWork.CommitAsync(ct);

        return Result<PlayerResponse>.Ok(new PlayerResponse
        {
            Id        = player.Id,
            TeamId    = player.TeamId,
            FullName  = player.FullName,
            Number    = player.Number,
            CreatedAt = player.CreatedAt
        });
    }
}
