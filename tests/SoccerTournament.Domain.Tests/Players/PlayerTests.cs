using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Players.Events;

namespace SoccerTournament.Domain.Tests.Players;

public class PlayerTests
{
    private static readonly Guid TeamId = Guid.NewGuid();

    [Fact]
    public void Register_WithValidData_ShouldReturnPlayerAndRaiseEvent()
    {
        var result = Player.Register(TeamId, " Juan ", " Pérez ", 10, PlayerPosition.Forward);

        Assert.True(result.IsSuccess);
        Assert.Equal("Juan", result.Value.FirstName);
        Assert.Equal("Pérez", result.Value.LastName);
        Assert.Equal(10, result.Value.JerseyNumber);
        Assert.Equal(TeamId, result.Value.TeamId);

        var domainEvent = Assert.IsType<PlayerRegisteredDomainEvent>(Assert.Single(result.Value.DomainEvents));
        Assert.Equal(TeamId, domainEvent.TeamId);
    }

    [Fact]
    public void Register_WithBlankFirstName_ShouldNormalizeToNull()
    {
        var result = Player.Register(TeamId, "  ", "Pérez", 1, PlayerPosition.Goalkeeper);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.FirstName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Register_WithJerseyNumberOutOfRange_ShouldFail(int jerseyNumber)
    {
        var result = Player.Register(TeamId, "Juan", "Pérez", jerseyNumber, PlayerPosition.Defender);

        Assert.Equal(PlayerErrors.JerseyNumberOutOfRange, result.Error);
    }

    [Fact]
    public void Register_WithoutLastName_ShouldFail()
    {
        var result = Player.Register(TeamId, "Juan", "", 5, PlayerPosition.Midfielder);

        Assert.Equal(PlayerErrors.LastNameRequired, result.Error);
    }

    [Fact]
    public void Register_WithUndefinedPosition_ShouldFail()
    {
        var result = Player.Register(TeamId, "Juan", "Pérez", 5, (PlayerPosition)99);

        Assert.Equal(PlayerErrors.InvalidPosition, result.Error);
    }

    [Fact]
    public void Register_WithoutTeam_ShouldFail()
    {
        var result = Player.Register(Guid.Empty, "Juan", "Pérez", 5, PlayerPosition.Midfielder);

        Assert.Equal(PlayerErrors.TeamRequired, result.Error);
    }

    [Fact]
    public void Register_WithSeveralInvalidFields_ShouldReturnValidationError()
    {
        var result = Player.Register(Guid.Empty, null, "", 0, PlayerPosition.Forward);

        var validationError = Assert.IsType<ValidationError>(result.Error);
        Assert.Equal(3, validationError.Errors.Count);
    }
}
