using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Domain.Teams.Events;

namespace SoccerTournament.Domain.Tests.Teams;

public class TeamTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnTeamAndRaiseEvent()
    {
        var result = Team.Create("  Club Deportivo Ejemplo  ", " CDE ", new DateTime(1921, 5, 10));

        Assert.True(result.IsSuccess);
        Assert.Equal("Club Deportivo Ejemplo", result.Value.FullName);
        Assert.Equal("CDE", result.Value.ShortName);
        Assert.NotEqual(Guid.Empty, result.Value.Id);

        var domainEvent = Assert.IsType<TeamCreatedDomainEvent>(Assert.Single(result.Value.DomainEvents));
        Assert.Equal(result.Value.Id, domainEvent.TeamId);
    }

    [Fact]
    public void Create_WithoutFoundedYear_ShouldSucceed()
    {
        var result = Team.Create("Club Sin Fecha", "CSF", null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.FoundedYear);
    }

    [Theory]
    [InlineData("", "CDE")]
    [InlineData("   ", "CDE")]
    public void Create_WithoutFullName_ShouldFailWithValidation(string fullName, string shortName)
    {
        var result = Team.Create(fullName, shortName, null);

        Assert.True(result.IsFailure);
        Assert.Equal(TeamErrors.FullNameRequired, result.Error);
    }

    [Fact]
    public void Create_WithFullNameTooLong_ShouldFail()
    {
        var result = Team.Create(new string('A', Team.FullNameMaxLength + 1), "CDE", null);

        Assert.Equal(TeamErrors.FullNameTooLong, result.Error);
    }

    [Theory]
    [InlineData(1849)]
    [InlineData(2101)]
    public void Create_WithFoundedYearOutOfRange_ShouldFail(int year)
    {
        var result = Team.Create("Club Ejemplo", "CE", new DateTime(year, 1, 1));

        Assert.Equal(TeamErrors.FoundedYearOutOfRange, result.Error);
    }

    [Fact]
    public void Create_WithSeveralInvalidFields_ShouldReturnValidationErrorWithAllErrors()
    {
        var result = Team.Create("", "", new DateTime(1800, 1, 1));

        var validationError = Assert.IsType<ValidationError>(result.Error);
        Assert.Equal(ErrorType.Validation, validationError.Type);
        Assert.Equal(3, validationError.Errors.Count);
    }

    [Fact]
    public void Update_WithValidData_ShouldReplaceValuesAndRaiseEvent()
    {
        var team = Team.Create("Club Original", "CO", null).Value;
        team.ClearDomainEvents();

        var result = team.Update("Club Renombrado", "CR", new DateTime(1950, 1, 1));

        Assert.True(result.IsSuccess);
        Assert.Equal("Club Renombrado", team.FullName);
        Assert.Equal("CR", team.ShortName);
        Assert.IsType<TeamUpdatedDomainEvent>(Assert.Single(team.DomainEvents));
    }

    [Fact]
    public void Update_WithSameData_ShouldBeIdempotentWithoutRaisingEvent()
    {
        var team = Team.Create("Club Original", "CO", new DateTime(1950, 1, 1)).Value;
        team.ClearDomainEvents();

        var result = team.Update(" Club Original ", "CO", new DateTime(1950, 1, 1));

        Assert.True(result.IsSuccess);
        Assert.Empty(team.DomainEvents);
    }

    [Fact]
    public void Update_WithInvalidData_ShouldNotModifyTeam()
    {
        var team = Team.Create("Club Original", "CO", null).Value;
        team.ClearDomainEvents();

        var result = team.Update("", "CR", null);

        Assert.True(result.IsFailure);
        Assert.Equal("Club Original", team.FullName);
        Assert.Empty(team.DomainEvents);
    }
}
