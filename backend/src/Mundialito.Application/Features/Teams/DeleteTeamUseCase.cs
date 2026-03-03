using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Features.Teams;

/// <summary>
/// Caso de uso: Eliminar un equipo.
/// DELETE es idempotente puro (siempre 204).
/// Por eso NO se valida existencia del equipo; si el recurso no existe simplemente no hay nada que borrar.
/// Si Infrastructure detecta dependencias FK, retornará TEAM_HAS_DEPENDENCIES (409).
/// </summary>
public sealed class DeleteTeamUseCase
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork     _unitOfWork;

    public DeleteTeamUseCase(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
    {
        _teamRepository = teamRepository;
        _unitOfWork     = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        // DELETE idempotente: si no existe, retornamos éxito (API devuelve 204).
        var team = await _teamRepository.GetByIdAsync(id, ct);
        if (team is null)
            return Result.Ok();

        _teamRepository.Remove(team);
        await _unitOfWork.CommitAsync(ct);

        return Result.Ok();
    }
}
