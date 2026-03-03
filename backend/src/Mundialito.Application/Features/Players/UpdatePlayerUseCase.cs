using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Application.DTOs.Players;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Features.Players;

/// <summary>
/// Caso de uso: Actualizar datos de un jugador.
/// Validaciones:
///   - jugador debe existir → PLAYER_NOT_FOUND (404)
///   - fullName requerido → VALIDATION_ERROR (400)
/// </summary>
public sealed class UpdatePlayerUseCase
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IUnitOfWork       _unitOfWork;

    public UpdatePlayerUseCase(IPlayerRepository playerRepository, IUnitOfWork unitOfWork)
    {
        _playerRepository = playerRepository;
        _unitOfWork       = unitOfWork;
    }

    public async Task<Result<PlayerResponse>> ExecuteAsync(
        Guid                playerId,
        UpdatePlayerRequest request,
        CancellationToken   ct = default)
    {
        // 1. El jugador debe existir.
        var player = await _playerRepository.GetByIdAsync(playerId, ct);
        if (player is null)
            return Result<PlayerResponse>.Fail(
                DomainErrors.PlayerNotFound,
                $"Player '{playerId}' was not found.");

        // 2. fullName requerido.
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Result<PlayerResponse>.Fail(
                DomainErrors.ValidationError,
                "Player full name is required.");

        // 3. Aplicar cambio vía método de dominio.
        var updateResult = player.Update(request.FullName, request.Number);
        if (updateResult.IsFailure)
            return Result<PlayerResponse>.Fail(updateResult.ErrorCode!, updateResult.ErrorMessage!);

        _playerRepository.Update(player);
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
