using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Application.Features.Scorers.Queries;
using SoccerTournament.Infrastructure.ReadSide.Sql;

namespace SoccerTournament.Infrastructure.ReadSide;

/// <summary>
/// Tabla de goleadores con Dapper. Agrupa en SQL Server (aprovecha ix_goals_player) y calcula
/// el ranking con DENSE_RANK: jugadores con la misma cantidad de goles comparten puesto.
/// </summary>
internal sealed class ScorerReadService : IScorerReadService
{
    internal static readonly SortMap Sort = new(
        new Dictionary<string, string>
        {
            ["goals"] = "r.goals",
            ["playerName"] = "r.player_name",
            ["teamName"] = "r.team_name"
        },
        defaultOrderBy: "r.goals DESC, r.last_name ASC",
        tieBreaker: "r.player_id ASC",
        descendingByDefault: new[] { "goals" });

    private readonly SqlConnectionFactory _connectionFactory;

    public ScorerReadService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<TopScorerResponse>> GetPagedAsync(GetTopScorersQuery query, CancellationToken cancellationToken)
    {
        var filter = new SqlFilter()
            .Add(query.TeamId.HasValue, "g.team_id = @TeamId", "TeamId", query.TeamId);

        var where = filter.ToWhereClause();

        var pageSql = $"""
            WITH scorer_goals AS (
                SELECT g.player_id, g.team_id, COUNT_BIG(*) AS goals
                FROM dbo.goals g
                {where}
                GROUP BY g.player_id, g.team_id
            ),
            ranked AS (
                SELECT
                    sg.player_id,
                    sg.team_id,
                    sg.goals,
                    DENSE_RANK() OVER (ORDER BY sg.goals DESC) AS scorer_rank,
                    ISNULL(p.first_name + N' ', N'') + p.last_name AS player_name,
                    p.last_name,
                    tm.full_name AS team_name
                FROM scorer_goals sg
                INNER JOIN dbo.players p ON p.uuid = sg.player_id
                INNER JOIN dbo.teams tm ON tm.uuid = sg.team_id
            )
            SELECT
                r.scorer_rank AS Rank,
                r.player_id   AS PlayerId,
                r.player_name AS PlayerName,
                r.team_id     AS TeamId,
                r.team_name   AS TeamName,
                r.goals       AS Goals
            FROM ranked r
            {Sort.ToOrderByClause(query.SortBy, query.SortDirection)}
            """;

        var countSql = $"""
            SELECT COUNT(*)
            FROM (
                SELECT g.player_id
                FROM dbo.goals g
                {where}
                GROUP BY g.player_id, g.team_id
            ) scorers
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await PagedQuery.ExecuteAsync<ScorerRow, TopScorerResponse>(connection, pageSql, countSql, filter, query, ToResponse, cancellationToken);
    }

    private static TopScorerResponse ToResponse(ScorerRow row) =>
        new((int)row.Rank, row.PlayerId, row.PlayerName, row.TeamId, row.TeamName, (int)row.Goals);

    // DENSE_RANK y COUNT_BIG devuelven BIGINT
    private sealed class ScorerRow
    {
        public long Rank { get; set; }

        public Guid PlayerId { get; set; }

        public string PlayerName { get; set; } = null!;

        public Guid TeamId { get; set; }

        public string TeamName { get; set; } = null!;

        public long Goals { get; set; }
    }
}
