using Mundialito.Application.Abstractions;
using Mundialito.Application.Abstractions.Repositories;
using Mundialito.Application.DTOs.Teams;
using Mundialito.Domain.SeedWork;
using Mundialito.Domain.Teams;

namespace Mundialito.Application.Features.Teams;

/// <summary>
/// Caso de uso: Crear un nuevo equipo.
/// Validaciones:
///   - name requerido → VALIDATION_ERROR (400)
///   - nombre duplicado → TEAM_NAME_CONFLICT (409)
/// Emite: TeamCreatedEvent (via dominio).
/// </summary>
public sealed class CreateTeamUseCase
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork     _unitOfWork;

    public CreateTeamUseCase(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
    {
        _teamRepository = teamRepository;
        _unitOfWork     = unitOfWork;
    }

    public async Task<Result<TeamResponse>> ExecuteAsync(
        CreateTeamRequest request,
        CancellationToken ct = default)
    {
        // 1. Validación de nombre requerido (domain factory también lo valida,
        //    pero devolvemos el mismo error desde app para homogeneidad).
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<TeamResponse>.Fail(
                DomainErrors.ValidationError,
                "Team name is required.");

        // 2. Unicidad del nombre (Application responsibility).
        var existing = await _teamRepository.GetByNameAsync(request.Name.Trim(), ct);
        if (existing is not null)
            return Result<TeamResponse>.Fail(
                DomainErrors.TeamNameConflict,
                "A team with the same name already exists.");

        // 3. Crear entidad vía factory de dominio.
        var createResult = Team.Create(request.Name);
        if (createResult.IsFailure)
            return Result<TeamResponse>.Fail(createResult.ErrorCode!, createResult.ErrorMessage!);

        var team = createResult.Value;

        // 4. Persistir y commitear.
        _teamRepository.Add(team);
        await _unitOfWork.CommitAsync(ct);

        // 5. Proyectar respuesta.
        return Result<TeamResponse>.Ok(new TeamResponse
        {
            Id        = team.Id,
            Name      = team.Name,
            CreatedAt = team.CreatedAt
        });
    }
}
