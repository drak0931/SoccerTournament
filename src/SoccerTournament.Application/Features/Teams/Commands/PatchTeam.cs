using FluentValidation;
using SoccerTournament.Application.Abstractions;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Application.Features.Teams.Queries;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Features.Teams.Commands;

/// <summary>
/// Modificación parcial del equipo (PATCH). Solo cambian los campos enviados;
/// FoundedYear enviado como null limpia la fecha de fundación.
/// </summary>
public sealed record PatchTeamCommand(
    Guid TeamId,
    Optional<string> FullName,
    Optional<string> ShortName,
    Optional<DateTime?> FoundedYear) : ICommand<TeamResponse>;

internal sealed class PatchTeamCommandValidator : AbstractValidator<PatchTeamCommand>
{
    public PatchTeamCommandValidator()
    {
        RuleFor(command => command.TeamId).NotEmpty();

        RuleFor(command => command)
            .Must(command => command.FullName.HasValue || command.ShortName.HasValue || command.FoundedYear.HasValue)
            .WithName("Body")
            .WithMessage("Debe enviar al menos un campo para modificar.");

        RuleFor(command => command.FullName.Value)
            .NotEmpty().MaximumLength(Team.FullNameMaxLength)
            .OverridePropertyName(nameof(PatchTeamCommand.FullName))
            .When(command => command.FullName.HasValue);

        RuleFor(command => command.ShortName.Value)
            .NotEmpty().MaximumLength(Team.ShortNameMaxLength)
            .OverridePropertyName(nameof(PatchTeamCommand.ShortName))
            .When(command => command.ShortName.HasValue);
    }
}

internal sealed class PatchTeamCommandHandler : ICommandHandler<PatchTeamCommand, TeamResponse>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PatchTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
    {
        _teamRepository = teamRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TeamResponse>> HandleAsync(PatchTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
        {
            return TeamErrors.NotFound(command.TeamId);
        }

        // Combina lo enviado con el estado actual y reutiliza la misma regla de negocio que PUT
        var fullName = command.FullName.GetValueOrDefault(team.FullName);
        var shortName = command.ShortName.GetValueOrDefault(team.ShortName);
        var foundedYear = command.FoundedYear.GetValueOrDefault(team.FoundedYear);

        var uniqueness = await TeamNameRules.EnsureUniqueAsync(_teamRepository, fullName, shortName, team.Id, cancellationToken);
        if (uniqueness.IsFailure)
        {
            return uniqueness.Error;
        }

        var update = team.Update(fullName, shortName, foundedYear);
        if (update.IsFailure)
        {
            return update.Error;
        }

        var commit = await _unitOfWork.CommitAsync(cancellationToken);
        if (commit.IsFailure)
        {
            return commit.Error;
        }

        return new TeamResponse(team.Id, team.FullName, team.ShortName, team.FoundedYear);
    }
}
