using FluentValidation;
using Microsoft.Extensions.Logging;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Features.Teams.Commands;

/// <summary>
/// Elimina un equipo (DELETE). Es idempotente: si el equipo ya no existe se considera éxito (204),
/// porque el estado final solicitado (equipo inexistente) ya se cumple.
/// </summary>
public sealed record DeleteTeamCommand(Guid TeamId) : ICommand;

internal sealed class DeleteTeamCommandValidator : AbstractValidator<DeleteTeamCommand>
{
    public DeleteTeamCommandValidator()
    {
        RuleFor(command => command.TeamId).NotEmpty();
    }
}

internal sealed class DeleteTeamCommandHandler : ICommandHandler<DeleteTeamCommand>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteTeamCommandHandler> _logger;

    public DeleteTeamCommandHandler(ITeamRepository teamRepository, IUnitOfWork unitOfWork, ILogger<DeleteTeamCommandHandler> logger)
    {
        _teamRepository = teamRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(DeleteTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
        {
            _logger.LogInformation("Equipo {TeamId} ya no existe; eliminación idempotente sin cambios", command.TeamId);
            return Result.Success();
        }

        if (await _teamRepository.HasDependenciesAsync(team.Id, cancellationToken))
        {
            return TeamErrors.HasDependencies;
        }

        team.MarkAsDeleted();
        _teamRepository.Remove(team);

        return await _unitOfWork.CommitAsync(cancellationToken);
    }
}
