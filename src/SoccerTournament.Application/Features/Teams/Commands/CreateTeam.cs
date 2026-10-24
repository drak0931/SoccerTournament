using FluentValidation;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Application.Features.Teams.Queries;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Standings;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Features.Teams.Commands;

/// <summary>
/// Registra un equipo y su fila inicial en la tabla de posiciones (POST, 201).
/// </summary>
public sealed record CreateTeamCommand(string FullName, string ShortName, DateTime? FoundedYear) : ICommand<TeamResponse>;

internal sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(Team.FullNameMaxLength);
        RuleFor(command => command.ShortName).NotEmpty().MaximumLength(Team.ShortNameMaxLength);
    }
}

internal sealed class CreateTeamCommandHandler : ICommandHandler<CreateTeamCommand, TeamResponse>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IStandingRepository _standingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTeamCommandHandler(ITeamRepository teamRepository, IStandingRepository standingRepository, IUnitOfWork unitOfWork)
    {
        _teamRepository = teamRepository;
        _standingRepository = standingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TeamResponse>> HandleAsync(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        var uniqueness = await TeamNameRules.EnsureUniqueAsync(_teamRepository, command.FullName, command.ShortName, null, cancellationToken);
        if (uniqueness.IsFailure)
        {
            return uniqueness.Error;
        }

        var teamResult = Team.Create(command.FullName, command.ShortName, command.FoundedYear);
        if (teamResult.IsFailure)
        {
            return teamResult.Error;
        }

        var team = teamResult.Value;

        // Dos repositorios, una sola transacción: el equipo y su fila de posiciones se confirman juntos
        _teamRepository.Add(team);
        _standingRepository.Add(Standing.CreateFor(team.Id));

        var commit = await _unitOfWork.CommitAsync(cancellationToken);
        if (commit.IsFailure)
        {
            return commit.Error;
        }

        return new TeamResponse(team.Id, team.FullName, team.ShortName, team.FoundedYear);
    }
}
