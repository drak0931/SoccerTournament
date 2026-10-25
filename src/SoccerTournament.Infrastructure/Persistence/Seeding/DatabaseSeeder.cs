using Microsoft.Extensions.Logging;
using SoccerTournament.Application.Abstractions.Clock;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Infrastructure.Persistence.Seeding;

/// <summary>
/// Carga los datos de prueba si la BD no tiene equipos. Todo se confirma en UNA transacción del
/// Unit of Work: o queda el seed completo (con sus eventos de dominio) o no queda nada.
/// </summary>
internal sealed class DatabaseSeeder
{
    private readonly ITeamRepository _teamRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IStandingRepository _standingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ITeamRepository teamRepository,
        IPlayerRepository playerRepository,
        IMatchRepository matchRepository,
        IStandingRepository standingRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ILogger<DatabaseSeeder> logger)
    {
        _teamRepository = teamRepository;
        _playerRepository = playerRepository;
        _matchRepository = matchRepository;
        _standingRepository = standingRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result> SeedAsync(CancellationToken cancellationToken)
    {
        if (await _teamRepository.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("La BD ya contiene equipos; se omite el seed de datos");
            return Result.Success();
        }

        var planResult = SeedPlan.Build(SeedData.Teams, SeedData.Matches, _dateTimeProvider.UtcNow);
        if (planResult.IsFailure)
        {
            _logger.LogError("Los datos del seed no cumplen las reglas de dominio: {ErrorCode} {ErrorMessage}",
                planResult.Error.Code, planResult.Error.Message);
            return planResult.Error;
        }

        var plan = planResult.Value;

        // Cuatro repositorios, una sola unidad de trabajo
        foreach (var team in plan.Teams)
        {
            _teamRepository.Add(team);
        }

        foreach (var standing in plan.Standings)
        {
            _standingRepository.Add(standing);
        }

        foreach (var player in plan.Players)
        {
            _playerRepository.Add(player);
        }

        foreach (var match in plan.Matches)
        {
            _matchRepository.Add(match);
        }

        var commit = await _unitOfWork.CommitAsync(cancellationToken);
        if (commit.IsFailure)
        {
            // Ej.: otra instancia de la API completó el seed al mismo tiempo (conflicto de unicidad)
            _logger.LogWarning("No se confirmó el seed de datos: {ErrorCode} {ErrorMessage}", commit.Error.Code, commit.Error.Message);
            return commit;
        }

        _logger.LogInformation(
            "Seed de datos completado: {TeamCount} equipos, {PlayerCount} jugadores, {MatchCount} partidos, {ResultCount} resultados",
            plan.Teams.Count, plan.Players.Count, plan.Matches.Count, plan.ResultsCount);

        return Result.Success();
    }
}
