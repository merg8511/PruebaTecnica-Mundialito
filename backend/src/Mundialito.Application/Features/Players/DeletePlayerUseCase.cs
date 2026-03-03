using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Features.Players;

/// <summary>
/// Caso de uso: Eliminar un jugador.
/// DELETE idempotente puro: si no existe, retorna éxito (API devuelve 204).
/// </summary>
public sealed class DeletePlayerUseCase
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IUnitOfWork       _unitOfWork;

    public DeletePlayerUseCase(IPlayerRepository playerRepository, IUnitOfWork unitOfWork)
    {
        _playerRepository = playerRepository;
        _unitOfWork       = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid playerId, CancellationToken ct = default)
    {
        var player = await _playerRepository.GetByIdAsync(playerId, ct);
        if (player is null)
            return Result.Ok(); // idempotente: no existe → igual 204

        _playerRepository.Remove(player);
        await _unitOfWork.CommitAsync(ct);

        return Result.Ok();
    }
}
