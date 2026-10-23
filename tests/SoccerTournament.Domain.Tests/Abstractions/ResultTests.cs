using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Domain.Tests.Abstractions;

public class ResultTests
{
    [Fact]
    public void Success_ShouldNotContainError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Failure_ShouldExposeMessageAndCode()
    {
        var error = Error.NotFound("No existe.", "Test.NotFound");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal("No existe.", result.ErrorMessage);
        Assert.Equal("Test.NotFound", result.ErrorCode);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public void Failure_WithoutCode_ShouldHaveNullErrorCode()
    {
        var result = Result.Failure(Error.Validation("Dato inválido."));

        Assert.True(result.IsFailure);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void GenericSuccess_ShouldExposeValue()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_ImplicitFromError_ShouldBeFailure()
    {
        Result<int> result = Error.Conflict("Duplicado.", "Test.Conflict");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public void GenericFailure_AccessingValue_ShouldBeProgrammingError()
    {
        var result = Result.Failure<int>(Error.Failure("Falló."));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
