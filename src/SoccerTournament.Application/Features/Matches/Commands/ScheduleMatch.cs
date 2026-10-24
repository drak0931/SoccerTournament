using FluentValidation;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Application.Features.Matches.Queries;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Features.Matches.Commands;

/// <summary>
/// Crea (programa) un partido en el calendario (POST /matches, 201).
/// </summary>
public sealed record ScheduleMatchCommand(Guid HomeTeamId, Guid AwayTeamId, DateTime ScheduledAt) : ICommand<MatchResponse>;

internal sealed class ScheduleMatchCommandValidator : AbstractValidator<ScheduleMatchCommand>
{
    public ScheduleMatchCommandValidator()
    {
        RuleFor(command => command.HomeTeamId).NotEmpty();
        RuleFor(command => command.AwayTeamId).NotEmpty();
        RuleFor(command => command.ScheduledAt).NotEmpty();
        RuleFor(command => command.AwayTeamId)
            .NotEqual(command => command.HomeTeamId)
            .WithMessage("Un equipo no puede jugar contra sí mismo.");
    }
}

internal sealed class ScheduleMatchCommandHandler : ICommandHandler<ScheduleMatchCommand, MatchResponse>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ScheduleMatchCommandHandler(ITeamRepository teamRepository, IMatchRepository matchRepository, IUnitOfWork unitOfWork)
    {
        _teamRepository = teamRepository;
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MatchResponse>> HandleAsync(ScheduleMatchCommand command, CancellationToken cancellationToken)
    {
        var homeTeam = await _teamRepository.GetByIdAsync(command.HomeTeamId, cancellationToken);
        if (homeTeam is null)
        {
            return TeamErrors.NotFound(command.HomeTeamId);
        }

        var awayTeam = await _teamRepository.GetByIdAsync(command.AwayTeamId, cancellationToken);
        if (awayTeam is null)
        {
            return TeamErrors.NotFound(command.AwayTeamId);
        }

        var matchResult = Match.Schedule(homeTeam.Id, awayTeam.Id, command.ScheduledAt);
        if (matchResult.IsFailure)
        {
            return matchResult.Error;
        }

        var match = matchResult.Value;
        _matchRepository.Add(match);

        var commit = await _unitOfWork.CommitAsync(cancellationToken);
        if (commit.IsFailure)
        {
            return commit.Error;
        }

        return new MatchResponse(
            match.Id,
            homeTeam.Id,
            homeTeam.FullName,
            awayTeam.Id,
            awayTeam.FullName,
            match.ScheduledAt,
            match.Status,
            match.HomeGoals,
            match.AwayGoals,
            match.ResultRegisteredAt);
    }
}
