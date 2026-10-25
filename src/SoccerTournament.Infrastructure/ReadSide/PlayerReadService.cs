using Dapper;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Application.Features.Players.Queries;
using SoccerTournament.Domain.Players;
using SoccerTournament.Infrastructure.ReadSide.Sql;

namespace SoccerTournament.Infrastructure.ReadSide;

/// <summary>
/// Consultas de jugadores con Dapper.
/// </summary>
internal sealed class PlayerReadService : IPlayerReadService
{
    internal static readonly SortMap Sort = new(
        new Dictionary<string, string>
        {
            ["lastName"] = "p.last_name",
            ["firstName"] = "p.first_name",
            ["jerseyNumber"] = "p.jersey_number",
            ["position"] = "p.position",
            ["teamName"] = "t.full_name"
        },
        defaultOrderBy: "p.last_name ASC, p.first_name ASC",
        tieBreaker: "p.uuid ASC");

    private const string SelectFrom = """
        SELECT
            p.uuid          AS Id,
            p.team_id       AS TeamId,
            t.full_name     AS TeamName,
            p.first_name    AS FirstName,
            p.last_name     AS LastName,
            p.jersey_number AS JerseyNumber,
            p.position      AS Position
        FROM dbo.players p
        INNER JOIN dbo.teams t ON t.uuid = p.team_id
        """;

    private readonly SqlConnectionFactory _connectionFactory;

    public PlayerReadService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<PlayerResponse>> GetPagedAsync(GetPlayersQuery query, CancellationToken cancellationToken)
    {
        var filter = new SqlFilter()
            .Add(query.TeamId.HasValue, "p.team_id = @TeamId", "TeamId", query.TeamId)
            .Add(!string.IsNullOrWhiteSpace(query.Name),
                "(p.first_name LIKE @Name OR p.last_name LIKE @Name)", "Name",
                query.Name is null ? null : SqlFilter.ContainsPattern(query.Name))
            // position es VARCHAR: parámetro ANSI para no forzar conversión implícita de la columna
            .Add(query.Position.HasValue, "p.position = @Position", "Position",
                new DbString { Value = query.Position?.ToString(), IsAnsi = true, Length = 20 });

        var where = filter.ToWhereClause();

        var pageSql = $"""
            {SelectFrom}
            {where}
            {Sort.ToOrderByClause(query.SortBy, query.SortDirection)}
            """;

        // Todos los filtros son sobre players: el conteo no necesita el JOIN
        var countSql = $"SELECT COUNT(*) FROM dbo.players p {where}";

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await PagedQuery.ExecuteAsync<PlayerRow, PlayerResponse>(connection, pageSql, countSql, filter, query, ToResponse, cancellationToken);
    }

    public async Task<PlayerResponse?> GetByIdAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var sql = $"{SelectFrom} WHERE p.uuid = @PlayerId";

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PlayerRow>(
            new CommandDefinition(sql, new { PlayerId = playerId }, commandTimeout: PagedQuery.CommandTimeoutSeconds, cancellationToken: cancellationToken));

        return row is null ? null : ToResponse(row);
    }

    private static PlayerResponse ToResponse(PlayerRow row) =>
        new(row.Id, row.TeamId, row.TeamName, row.FirstName, row.LastName, row.JerseyNumber, Enum.Parse<PlayerPosition>(row.Position, ignoreCase: true));

    private sealed class PlayerRow
    {
        public Guid Id { get; set; }

        public Guid TeamId { get; set; }

        public string TeamName { get; set; } = null!;

        public string? FirstName { get; set; }

        public string LastName { get; set; } = null!;

        public byte JerseyNumber { get; set; }

        public string Position { get; set; } = null!;
    }
}
