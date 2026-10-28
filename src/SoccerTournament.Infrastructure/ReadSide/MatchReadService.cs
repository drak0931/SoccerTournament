using Dapper;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Application.Features.Matches.Queries;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Infrastructure.ReadSide.Sql;

namespace SoccerTournament.Infrastructure.ReadSide;

/// <summary>
/// Consultas de partidos con Dapper (filtros por fecha, equipo y estado).
/// </summary>
internal sealed class MatchReadService : IMatchReadService
{
    internal static readonly SortMap Sort = new(
        new Dictionary<string, string>
        {
            ["scheduledAt"] = "m.scheduled_at",
            ["status"] = "m.status",
            ["createdAt"] = "m.created_at"
        },
        defaultOrderBy: "m.scheduled_at ASC",
        tieBreaker: "m.uuid ASC",
        descendingByDefault: new[] { "createdAt" });

    private const string SelectFrom = """
        SELECT
            m.uuid                 AS Id,
            m.home_team_id         AS HomeTeamId,
            ht.full_name           AS HomeTeamName,
            m.away_team_id         AS AwayTeamId,
            awt.full_name          AS AwayTeamName,
            m.scheduled_at         AS ScheduledAt,
            m.status               AS Status,
            m.home_goals           AS HomeGoals,
            m.away_goals           AS AwayGoals,
            m.result_registered_at AS ResultRegisteredAt
        FROM dbo.matches m
        INNER JOIN dbo.teams ht  ON ht.uuid  = m.home_team_id
        INNER JOIN dbo.teams awt ON awt.uuid = m.away_team_id
        """;

    private const string GoalsSql = """
        SELECT
            g.uuid      AS Id,
            g.player_id AS PlayerId,
            ISNULL(p.first_name + N' ', N'') + p.last_name AS PlayerName,
            g.team_id   AS TeamId,
            g.minute    AS Minute
        FROM dbo.goals g
        INNER JOIN dbo.players p ON p.uuid = g.player_id
        WHERE g.match_id = @MatchId
        ORDER BY g.minute ASC, g.uuid ASC
        """;

    private readonly SqlConnectionFactory _connectionFactory;

    public MatchReadService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<MatchResponse>> GetPagedAsync(GetMatchesQuery query, CancellationToken cancellationToken)
    {
        // Una fecha sin hora en DateTo incluye todo ese día
        var dateToIsDateOnly = query.DateTo is { TimeOfDay.Ticks: 0 };

        var filter = new SqlFilter()
            .Add(query.DateFrom.HasValue, "m.scheduled_at >= @DateFrom", "DateFrom", query.DateFrom)
            .Add(query.DateTo.HasValue && dateToIsDateOnly, "m.scheduled_at < @DateToExclusive", "DateToExclusive", query.DateTo?.AddDays(1))
            .Add(query.DateTo.HasValue && !dateToIsDateOnly, "m.scheduled_at <= @DateTo", "DateTo", query.DateTo)
            .Add(query.TeamId.HasValue, "(m.home_team_id = @TeamId OR m.away_team_id = @TeamId)", "TeamId", query.TeamId)
            // status es VARCHAR: parámetro ANSI para aprovechar ix_matches_status
            .Add(query.Status.HasValue, "m.status = @Status", "Status",
                new DbString { Value = query.Status?.ToString().ToLowerInvariant(), IsAnsi = true, Length = 20 });

        var where = filter.ToWhereClause();

        var pageSql = $"""
            {SelectFrom}
            {where}
            {Sort.ToOrderByClause(query.SortBy, query.SortDirection)}
            """;

        var countSql = $"SELECT COUNT(*) FROM dbo.matches m {where}";

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await PagedQuery.ExecuteAsync<MatchRow, MatchResponse>(connection, pageSql, countSql, filter, query, ToResponse, cancellationToken);
    }

    public async Task<MatchDetailResponse?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken)
    {
        // Partido y goles en un solo viaje a la BD
        var sql = $"""
            {SelectFrom}
            WHERE m.uuid = @MatchId;

            {GoalsSql};
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { MatchId = matchId }, commandTimeout: PagedQuery.CommandTimeoutSeconds, cancellationToken: cancellationToken));

        var match = await results.ReadSingleOrDefaultAsync<MatchRow>();
        if (match is null)
        {
            return null;
        }

        var goals = (await results.ReadAsync<GoalRow>())
            .Select(goal => new GoalResponse(goal.Id, goal.PlayerId, goal.PlayerName, goal.TeamId, goal.Minute))
            .ToList();

        return new MatchDetailResponse(ToResponse(match), goals);
    }

    private static MatchResponse ToResponse(MatchRow row) =>
        new(
            row.Id,
            row.HomeTeamId,
            row.HomeTeamName,
            row.AwayTeamId,
            row.AwayTeamName,
            PagedQuery.AsUtc(row.ScheduledAt),
            Enum.Parse<MatchStatus>(row.Status, ignoreCase: true),
            row.HomeGoals,
            row.AwayGoals,
            PagedQuery.AsUtc(row.ResultRegisteredAt));

    private sealed class MatchRow
    {
        public Guid Id { get; set; }

        public Guid HomeTeamId { get; set; }

        public string HomeTeamName { get; set; } = null!;

        public Guid AwayTeamId { get; set; }

        public string AwayTeamName { get; set; } = null!;

        public DateTime ScheduledAt { get; set; }

        public string Status { get; set; } = null!;

        public byte? HomeGoals { get; set; }

        public byte? AwayGoals { get; set; }

        public DateTime? ResultRegisteredAt { get; set; }
    }

    private sealed class GoalRow
    {
        public Guid Id { get; set; }

        public Guid PlayerId { get; set; }

        public string PlayerName { get; set; } = null!;

        public Guid TeamId { get; set; }

        public byte Minute { get; set; }
    }
}
