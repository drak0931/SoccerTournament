using FluentValidation;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Application.Features.Teams.Queries;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Features.Teams.Commands;

/// <summary>
/// Reemplazo completo del equipo (PUT). Es idempotente: repetirlo con los mismos datos deja el mismo estado.
/// </summary>
public sealed record UpdateTeamCommand(Guid TeamId, string FullName, string ShortName, DateTime? FoundedYear) : ICommand<TeamResponse>;

internal sealed class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamCommandValidator()
    {
        RuleFor(command => command.TeamId).NotEmpty();
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(Team.FullNameMaxLength);
        RuleFor(command => command.ShortName).NotEmpty().MaximumLength(Team.ShortNameMaxLength);
    }
}

internal sealed class UpdateTeamCommandHandler : ICommandHandler<UpdateTeamCommand, TeamResponse>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork)
    {
        _teamRepository = teamRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TeamResponse>> HandleAsync(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
        {
            return TeamErrors.NotFound(command.TeamId);
        }

        var uniqueness = await TeamNameRules.EnsureUniqueAsync(_teamRepository, command.FullName, command.ShortName, team.Id, cancellationToken);
        if (uniqueness.IsFailure)
        {
            return uniqueness.Error;
        }

        var update = team.Update(command.FullName, command.ShortName, command.FoundedYear);
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
