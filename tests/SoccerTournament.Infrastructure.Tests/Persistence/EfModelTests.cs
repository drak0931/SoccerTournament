using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Standings;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Infrastructure.Persistence.DomainEvents;

namespace SoccerTournament.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifica que el modelo de EF Core coincide con el esquema de 0001_InitialSchema.sql.
/// </summary>
public class EfModelTests
{
    [Theory]
    [InlineData(typeof(Team), "teams")]
    [InlineData(typeof(Player), "players")]
    [InlineData(typeof(Match), "matches")]
    [InlineData(typeof(Goal), "goals")]
    [InlineData(typeof(Standing), "standings")]
    [InlineData(typeof(DomainEventRecord), "domain_events")]
    public void Entity_ShouldMapToExpectedTable(Type entityType, string tableName)
    {
        using var context = TestDbContextFactory.Create();

        var entity = context.Model.FindEntityType(entityType);

        Assert.NotNull(entity);
        Assert.Equal(tableName, entity.GetTableName());
        Assert.Equal("dbo", entity.GetSchema());
    }

    [Theory]
    [InlineData(typeof(Team), "uuid", "full_name", "short_name", "founded_year", "created_at", "created_by", "modified_at", "modified_by", "row_version")]
    [InlineData(typeof(Player), "uuid", "team_id", "first_name", "last_name", "jersey_number", "position", "created_at", "created_by", "row_version")]
    [InlineData(typeof(Match), "uuid", "home_team_id", "away_team_id", "scheduled_at", "status", "home_goals", "away_goals", "result_registered_at", "row_version")]
    [InlineData(typeof(Goal), "uuid", "match_id", "player_id", "team_id", "minute", "created_at", "created_by")]
    [InlineData(typeof(Standing), "team_id", "played", "won", "drawn", "lost", "goals_for", "goals_against", "goal_difference", "points", "modified_at", "row_version")]
    public void Entity_ShouldMapExpectedColumns(Type entityType, params string[] expectedColumns)
    {
        using var context = TestDbContextFactory.Create();
        var entity = context.Model.FindEntityType(entityType)!;
        var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());

        var mappedColumns = entity.GetProperties().Select(property => property.GetColumnName(table)).ToArray();

        Assert.All(expectedColumns, column => Assert.Contains(column, mappedColumns));
    }

    [Fact]
    public void FoundedYear_ShouldBeDatetimeColumn()
    {
        using var context = TestDbContextFactory.Create();

        var property = context.Model.FindEntityType(typeof(Team))!.FindProperty(nameof(Team.FoundedYear))!;

        Assert.Equal("datetime", property.GetColumnType());
    }

    [Fact]
    public void Model_ShouldGenerateCreateScriptWithoutErrors()
    {
        // Valida que relaciones, claves alternas y conversiones son consistentes (sin conectarse a la BD)
        using var context = TestDbContextFactory.Create();

        var script = context.Database.GenerateCreateScript();

        Assert.Contains("fk_goals_player_team", script);
        Assert.Contains("uq_players_uuid_team", script);
        Assert.Contains("fk_standings_teams", script);
    }
}
