using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Infrastructure.Persistence;

namespace SoccerTournament.Infrastructure.Tests.Persistence;

public class SqlErrorTranslatorTests
{
    [Theory]
    [InlineData(2627, "Violation of UNIQUE KEY constraint 'uq_teams_full_name'. Cannot insert duplicate key in object 'dbo.teams'.", "Team.FullNameAlreadyExists")]
    [InlineData(2627, "Violation of UNIQUE KEY constraint 'uq_teams_short_name'. Cannot insert duplicate key in object 'dbo.teams'.", "Team.ShortNameAlreadyExists")]
    [InlineData(2601, "Cannot insert duplicate key row in object 'dbo.players' with unique index 'uq_players_team_jersey'.", "Player.JerseyNumberTaken")]
    public void Translate_KnownUniqueConstraint_ShouldReturnSpecificConflict(int number, string message, string expectedCode)
    {
        var error = SqlErrorTranslator.Translate(number, message);

        Assert.NotNull(error);
        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal(expectedCode, error.Code);
    }

    [Fact]
    public void Translate_UnknownUniqueConstraint_ShouldReturnGenericDuplicate()
    {
        var error = SqlErrorTranslator.Translate(2627, "Violation of PRIMARY KEY constraint 'pk_other'.");

        Assert.Equal(SqlErrorTranslator.DuplicateRecord, error);
    }

    [Fact]
    public void Translate_ForeignKeyConflict_ShouldReturnConstraintViolation()
    {
        var error = SqlErrorTranslator.Translate(547, "The INSERT statement conflicted with the FOREIGN KEY constraint \"fk_goals_player_team\".");

        Assert.Equal(SqlErrorTranslator.ConstraintViolation, error);
    }

    [Fact]
    public void Translate_UnrelatedError_ShouldReturnNullSoItIsNotHidden()
    {
        var error = SqlErrorTranslator.Translate(1205, "Transaction was deadlocked.");

        Assert.Null(error);
    }

    [Fact]
    public void KnownErrors_ShouldMatchDomainCatalog()
    {
        Assert.Equal(TeamErrors.FullNameAlreadyExists, SqlErrorTranslator.Translate(2627, "uq_teams_full_name"));
        Assert.Equal(PlayerErrors.JerseyNumberTaken, SqlErrorTranslator.Translate(2627, "uq_players_team_jersey"));
    }
}
