using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Application.Features.Standings.Queries;
using SoccerTournament.Infrastructure.ReadSide.Sql;

namespace SoccerTournament.Infrastructure.ReadSide;

/// <summary>
/// Tabla de posiciones con Dapper. La posición oficial se calcula en SQL Server con ROW_NUMBER:
/// 1) Puntos  2) Diferencia de gol  3) Goles a favor  (4) nombre, solo como desempate estable).
/// </summary>
internal sealed class StandingReadService : IStandingReadService
{
    // En la consulta de página el alias "t" es el CTE ranked; en el conteo es dbo.teams (misma columna full_name)
    internal static readonly SortMap Sort = new(
        new Dictionary<string, string>
        {
            ["position"] = "t.position",
            ["points"] = "t.points",
            ["goalDifference"] = "t.goal_difference",
            ["goalsFor"] = "t.goals_for",
            ["teamName"] = "t.full_name"
        },
        defaultOrderBy: "t.position ASC",
        tieBreaker: "t.team_id ASC",
        descendingByDefault: new[] { "points", "goalDifference", "goalsFor" });

    private readonly SqlConnectionFactory _connectionFactory;

    public StandingReadService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<StandingResponse>> GetPagedAsync(GetStandingsQuery query, CancellationToken cancellationToken)
    {
        // El filtro por nombre se aplica DESPUÉS de calcular la posición, para no alterar el ranking
        var filter = new SqlFilter()
            .Add(!string.IsNullOrWhiteSpace(query.TeamName),
                "t.full_name LIKE @TeamName", "TeamName",
                query.TeamName is null ? null : SqlFilter.ContainsPattern(query.TeamName));

        var where = filter.ToWhereClause();

        var pageSql = $"""
            WITH ranked AS (
                SELECT
                    s.team_id,
                    tm.full_name,
                    tm.short_name,
                    s.played,
                    s.won,
                    s.drawn,
                    s.lost,
                    s.goals_for,
                    s.goals_against,
                    s.goal_difference,
                    s.points,
                    ROW_NUMBER() OVER (
                        ORDER BY s.points DESC, s.goal_difference DESC, s.goals_for DESC, tm.full_name ASC
                    ) AS position
                FROM dbo.standings s
                INNER JOIN dbo.teams tm ON tm.uuid = s.team_id
            )
            SELECT
                t.position        AS Position,
                t.team_id         AS TeamId,
                t.full_name       AS TeamName,
                t.short_name      AS TeamShortName,
                t.played          AS Played,
                t.won             AS Won,
                t.drawn           AS Drawn,
                t.lost            AS Lost,
                t.goals_for       AS GoalsFor,
                t.goals_against   AS GoalsAgainst,
                t.goal_difference AS GoalDifference,
                t.points          AS Points
            FROM ranked t
            {where}
            {Sort.ToOrderByClause(query.SortBy, query.SortDirection)}
            """;

        var countSql = $"""
            SELECT COUNT(*)
            FROM dbo.standings s
            INNER JOIN dbo.teams t ON t.uuid = s.team_id
            {where}
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await PagedQuery.ExecuteAsync<StandingRow, StandingResponse>(connection, pageSql, countSql, filter, query, ToResponse, cancellationToken);
    }

    private static StandingResponse ToResponse(StandingRow row) =>
        new(
            (int)row.Position,
            row.TeamId,
            row.TeamName,
            row.TeamShortName,
            row.Played,
            row.Won,
            row.Drawn,
            row.Lost,
            row.GoalsFor,
            row.GoalsAgainst,
            row.GoalDifference,
            row.Points);

    // Tipos alineados con la BD: SMALLINT → short, ROW_NUMBER → BIGINT
    private sealed class StandingRow
    {
        public long Position { get; set; }

        public Guid TeamId { get; set; }

        public string TeamName { get; set; } = null!;

        public string TeamShortName { get; set; } = null!;

        public short Played { get; set; }

        public short Won { get; set; }

        public short Drawn { get; set; }

        public short Lost { get; set; }

        public short GoalsFor { get; set; }

        public short GoalsAgainst { get; set; }

        public short GoalDifference { get; set; }

        public short Points { get; set; }
    }
}
