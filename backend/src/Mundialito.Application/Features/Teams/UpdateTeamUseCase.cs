using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Application.DTOs.Teams;
using Mundialito.Domain.SeedWork;

namespace Mundialito.Application.Features.Teams;

/// <summary>
/// Caso de uso: Actualizar nombre de un equipo existente.
/// Validaciones:
///   - name requerido → VALIDATION_ERROR (400)
///   - nombre duplicado (otro equipo) → TEAM_NAME_CONFLICT (409)
///   - equipo no existe → TEAM_NOT_FOUND (404)
/// </summary>
public sealed class UpdateTeamUseCase
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork     _unitOfWork;

    public UpdateTeamUseCase(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
    {
        _teamRepository = teamRepository;
        _unitOfWork     = unitOfWork;
    }

    public async Task<Result<TeamResponse>> ExecuteAsync(
        Guid              id,
        UpdateTeamRequest request,
        CancellationToken ct = default)
    {
        // 1. Validación de nombre requerido.
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<TeamResponse>.Fail(
                DomainErrors.ValidationError,
                "Team name is required.");

        // 2. El equipo debe existir.
        var team = await _teamRepository.GetByIdAsync(id, ct);
        if (team is null)
            return Result<TeamResponse>.Fail(
                DomainErrors.TeamNotFound,
                $"Team '{id}' was not found.");

        // 3. Unicidad de nombre (no contar el propio equipo).
        var trimmedName = request.Name.Trim();
        if (!string.Equals(team.Name, trimmedName, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _teamRepository.GetByNameAsync(trimmedName, ct);
            if (existing is not null)
                return Result<TeamResponse>.Fail(
                    DomainErrors.TeamNameConflict,
                    "A team with the same name already exists.");
        }

        // 4. Aplicar cambio vía método de dominio.
        var renameResult = team.Rename(trimmedName);
        if (renameResult.IsFailure)
            return Result<TeamResponse>.Fail(renameResult.ErrorCode!, renameResult.ErrorMessage!);

        _teamRepository.Update(team);
        await _unitOfWork.CommitAsync(ct);

        return Result<TeamResponse>.Ok(new TeamResponse
        {
            Id        = team.Id,
            Name      = team.Name,
            CreatedAt = team.CreatedAt
        });
    }
}
