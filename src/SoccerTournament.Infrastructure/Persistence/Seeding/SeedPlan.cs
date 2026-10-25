using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Standings;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Infrastructure.Persistence.Seeding;

/// <summary>
/// Construye en memoria los agregados del seed usando las MISMAS reglas de dominio que la API
/// (fábricas, invariantes, eventos y cálculo de posiciones). No toca la BD: es determinista y testeable.
/// </summary>
internal sealed class SeedPlan
{
    private SeedPlan(IReadOnlyList<Team> teams, IReadOnlyList<Standing> standings, IReadOnlyList<Player> players, IReadOnlyList<Match> matches)
    {
        Teams = teams;
        Standings = standings;
        Players = players;
        Matches = matches;
    }

    public IReadOnlyList<Team> Teams { get; }

    public IReadOnlyList<Standing> Standings { get; }

    public IReadOnlyList<Player> Players { get; }

    public IReadOnlyList<Match> Matches { get; }

    public int ResultsCount => Matches.Count(match => match.Status == MatchStatus.Finished);

    public static Result<SeedPlan> Build(
        IReadOnlyList<SeedData.TeamSeed> teamSeeds,
        IReadOnlyList<SeedData.MatchSeed> matchSeeds,
        DateTime utcNow)
    {
        var teams = new Dictionary<string, Team>(StringComparer.OrdinalIgnoreCase);
        var standings = new Dictionary<Guid, Standing>();
        var players = new Dictionary<(string Team, int Jersey), Player>();

        foreach (var teamSeed in teamSeeds)
        {
            var teamResult = Team.Create(teamSeed.FullName, teamSeed.ShortName, teamSeed.FoundedYear);
            if (teamResult.IsFailure)
            {
                return teamResult.Error;
            }

            var team = teamResult.Value;
            teams[team.ShortName] = team;
            standings[team.Id] = Standing.CreateFor(team.Id);

            foreach (var playerSeed in teamSeed.Players)
            {
                var playerResult = Player.Register(team.Id, playerSeed.FirstName, playerSeed.LastName, playerSeed.JerseyNumber, playerSeed.Position);
                if (playerResult.IsFailure)
                {
                    return playerResult.Error;
                }

                players[(team.ShortName, playerSeed.JerseyNumber)] = playerResult.Value;
            }
        }

        var seedDate = utcNow.Date;
        var matches = new List<Match>();

        foreach (var matchSeed in matchSeeds)
        {
            if (!teams.TryGetValue(matchSeed.HomeShortName, out var homeTeam) || !teams.TryGetValue(matchSeed.AwayShortName, out var awayTeam))
            {
                return Error.Failure($"El seed referencia un equipo inexistente en {matchSeed.HomeShortName} vs {matchSeed.AwayShortName}.", "Seed.UnknownTeam");
            }

            var scheduledAt = seedDate.AddDays(matchSeed.DaysFromSeedDate).Add(SeedData.KickOffTimeUtc);
            var matchResult = Match.Schedule(homeTeam.Id, awayTeam.Id, DateTime.SpecifyKind(scheduledAt, DateTimeKind.Utc));
            if (matchResult.IsFailure)
            {
                return matchResult.Error;
            }

            var match = matchResult.Value;
            matches.Add(match);

            if (matchSeed.Result is null)
            {
                continue;
            }

            var registration = RegisterResult(match, matchSeed.Result, teams, players, standings);
            if (registration.IsFailure)
            {
                return registration.Error;
            }
        }

        return new SeedPlan(teams.Values.ToList(), standings.Values.ToList(), players.Values.ToList(), matches);
    }

    // Misma secuencia que RegisterMatchResultCommandHandler: resultado del partido + posiciones de ambos equipos
    private static Result RegisterResult(
        Match match,
        SeedData.ResultSeed resultSeed,
        IReadOnlyDictionary<string, Team> teams,
        IReadOnlyDictionary<(string Team, int Jersey), Player> players,
        IReadOnlyDictionary<Guid, Standing> standings)
    {
        var goals = new List<ScoredGoal>();
        foreach (var goalSeed in resultSeed.Goals)
        {
            if (!players.TryGetValue((goalSeed.TeamShortName, goalSeed.JerseyNumber), out var scorer))
            {
                return Error.Failure($"El seed referencia un goleador inexistente ({goalSeed.TeamShortName} #{goalSeed.JerseyNumber}).", "Seed.UnknownScorer");
            }

            goals.Add(new ScoredGoal(scorer.Id, teams[goalSeed.TeamShortName].Id, goalSeed.Minute));
        }

        // El resultado se registra dos horas después del inicio del partido
        var registration = match.RegisterResult(resultSeed.HomeGoals, resultSeed.AwayGoals, goals, match.ScheduledAt.AddHours(2));
        if (registration.IsFailure)
        {
            return registration;
        }

        var homeUpdate = standings[match.HomeTeamId].ApplyResult(resultSeed.HomeGoals, resultSeed.AwayGoals);
        if (homeUpdate.IsFailure)
        {
            return homeUpdate;
        }

        return standings[match.AwayTeamId].ApplyResult(resultSeed.AwayGoals, resultSeed.HomeGoals);
    }
}
