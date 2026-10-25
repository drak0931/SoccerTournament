using Dapper;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Application.Features.Teams.Queries;
using SoccerTournament.Infrastructure.ReadSide.Sql;

namespace SoccerTournament.Infrastructure.ReadSide;

/// <summary>
/// Consultas de equipos con Dapper.
/// </summary>
internal sealed class TeamReadService : ITeamReadService
{
    internal static readonly SortMap Sort = new(
        new Dictionary<string, string>
        {
            ["fullName"] = "t.full_name",
            ["shortName"] = "t.short_name",
            ["foundedYear"] = "t.founded_year",
            ["createdAt"] = "t.created_at"
        },
        defaultOrderBy: "t.full_name ASC",
        tieBreaker: "t.uuid ASC");

    private const string SelectColumns = """
        t.uuid         AS Id,
        t.full_name    AS FullName,
        t.short_name   AS ShortName,
        t.founded_year AS FoundedYear
        """;

    private readonly SqlConnectionFactory _connectionFactory;

    public TeamReadService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<TeamResponse>> GetPagedAsync(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        var filter = new SqlFilter()
            .Add(!string.IsNullOrWhiteSpace(query.Name),
                "(t.full_name LIKE @Name OR t.short_name LIKE @Name)", "Name",
                query.Name is null ? null : SqlFilter.ContainsPattern(query.Name))
            // Rangos sobre la columna (sargable) en lugar de YEAR(founded_year)
            .Add(query.FoundedFrom.HasValue,
                "t.founded_year >= @FoundedFrom", "FoundedFrom",
                query.FoundedFrom.HasValue ? new DateTime(query.FoundedFrom.Value, 1, 1) : null)
            .Add(query.FoundedTo.HasValue,
                "t.founded_year < @FoundedToExclusive", "FoundedToExclusive",
                query.FoundedTo.HasValue ? new DateTime(query.FoundedTo.Value + 1, 1, 1) : null);

        var where = filter.ToWhereClause();

        var pageSql = $"""
            SELECT {SelectColumns}
            FROM dbo.teams t
            {where}
            {Sort.ToOrderByClause(query.SortBy, query.SortDirection)}
            """;

        var countSql = $"SELECT COUNT(*) FROM dbo.teams t {where}";

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await PagedQuery.ExecuteAsync<TeamRow, TeamResponse>(connection, pageSql, countSql, filter, query, ToResponse, cancellationToken);
    }

    public async Task<TeamResponse?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken)
    {
        var sql = $"SELECT {SelectColumns} FROM dbo.teams t WHERE t.uuid = @TeamId";

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<TeamRow>(
            new CommandDefinition(sql, new { TeamId = teamId }, commandTimeout: PagedQuery.CommandTimeoutSeconds, cancellationToken: cancellationToken));

        return row is null ? null : ToResponse(row);
    }

    private static TeamResponse ToResponse(TeamRow row) =>
        new(row.Id, row.FullName, row.ShortName, PagedQuery.AsUtc(row.FoundedYear));

    private sealed class TeamRow
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = null!;

        public string ShortName { get; set; } = null!;

        public DateTime? FoundedYear { get; set; }
    }
}
