using FluentValidation;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Application.Features.Players.Queries;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Features.Players.Commands;

/// <summary>
/// Registra un jugador en un equipo existente (POST /teams/{teamId}/players, 201).
/// </summary>
public sealed record RegisterPlayerCommand(
    Guid TeamId,
    string? FirstName,
    string LastName,
    int JerseyNumber,
    PlayerPosition Position) : ICommand<PlayerResponse>;

internal sealed class RegisterPlayerCommandValidator : AbstractValidator<RegisterPlayerCommand>
{
    public RegisterPlayerCommandValidator()
    {
        RuleFor(command => command.TeamId).NotEmpty();
        RuleFor(command => command.FirstName).MaximumLength(Player.NameMaxLength);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(Player.NameMaxLength);
        RuleFor(command => command.JerseyNumber).InclusiveBetween(Player.MinJerseyNumber, Player.MaxJerseyNumber);
        RuleFor(command => command.Position).IsInEnum();
    }
}

internal sealed class RegisterPlayerCommandHandler : ICommandHandler<RegisterPlayerCommand, PlayerResponse>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterPlayerCommandHandler(ITeamRepository teamRepository, IPlayerRepository playerRepository, IUnitOfWork unitOfWork)
    {
        _teamRepository = teamRepository;
        _playerRepository = playerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlayerResponse>> HandleAsync(RegisterPlayerCommand command, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
        {
            return TeamErrors.NotFound(command.TeamId);
        }

        // Respaldado en BD por uq_players_team_jersey ante condiciones de carrera
        if (await _playerRepository.JerseyNumberExistsAsync(team.Id, command.JerseyNumber, cancellationToken))
        {
            return PlayerErrors.JerseyNumberTaken;
        }

        var playerResult = Player.Register(team.Id, command.FirstName, command.LastName, command.JerseyNumber, command.Position);
        if (playerResult.IsFailure)
        {
            return playerResult.Error;
        }

        var player = playerResult.Value;
        _playerRepository.Add(player);

        var commit = await _unitOfWork.CommitAsync(cancellationToken);
        if (commit.IsFailure)
        {
            return commit.Error;
        }

        return new PlayerResponse(player.Id, team.Id, team.FullName, player.FirstName, player.LastName, player.JerseyNumber, player.Position);
    }
}
