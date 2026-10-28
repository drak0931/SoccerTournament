using FluentValidation;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches;

namespace SoccerTournament.Application.Features.Matches.Commands;

/// <summary>
/// Cambio de estado de un partido (PATCH). La regla de qué transiciones se permiten vive en el dominio
/// (<see cref="Match.ChangeStatus"/>): hoy solo la cancelación de un partido programado.
/// </summary>
public sealed record ChangeMatchStatusCommand(Guid MatchId, MatchStatus Status) : ICommand;

internal sealed class ChangeMatchStatusCommandValidator : AbstractValidator<ChangeMatchStatusCommand>
{
    public ChangeMatchStatusCommandValidator()
    {
        RuleFor(command => command.MatchId).NotEmpty();
        RuleFor(command => command.Status).IsInEnum();
    }
}

internal sealed class ChangeMatchStatusCommandHandler : ICommandHandler<ChangeMatchStatusCommand>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeMatchStatusCommandHandler(IMatchRepository matchRepository, IUnitOfWork unitOfWork)
    {
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(ChangeMatchStatusCommand command, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return MatchErrors.NotFound(command.MatchId);
        }

        var change = match.ChangeStatus(command.Status);
        if (change.IsFailure)
        {
            return change;
        }

        return await _unitOfWork.CommitAsync(cancellationToken);
    }
}
