using FluentValidation;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches;

namespace SoccerTournament.Application.Features.Matches.Commands;

/// <summary>
/// Cancela un partido programado. Un partido finalizado devuelve 409.
/// </summary>
public sealed record CancelMatchCommand(Guid MatchId) : ICommand;

internal sealed class CancelMatchCommandValidator : AbstractValidator<CancelMatchCommand>
{
    public CancelMatchCommandValidator()
    {
        RuleFor(command => command.MatchId).NotEmpty();
    }
}

internal sealed class CancelMatchCommandHandler : ICommandHandler<CancelMatchCommand>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelMatchCommandHandler(IMatchRepository matchRepository, IUnitOfWork unitOfWork)
    {
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(CancelMatchCommand command, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return MatchErrors.NotFound(command.MatchId);
        }

        var cancellation = match.Cancel();
        if (cancellation.IsFailure)
        {
            return cancellation;
        }

        return await _unitOfWork.CommitAsync(cancellationToken);
    }
}
