using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoccerTournament.Api.Tests.Infrastructure;
using SoccerTournament.Application.Features.Matches.Commands;
using SoccerTournament.Application.Features.Teams.Commands;
using SoccerTournament.Application.Features.Teams.Queries;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Api.Tests;

/// <summary>
/// Idempotencia en POST: header obligatorio, sin duplicados y misma respuesta ante repeticiones.
/// </summary>
public sealed class IdempotencyTests : IDisposable
{
    private static readonly TeamResponse Team = new(Guid.NewGuid(), "Club Deportivo Volcanes", "VOL", null);
    private static readonly object TeamBody = new { fullName = "Club Deportivo Volcanes", shortName = "VOL" };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public IdempotencyTests()
    {
        _client = _factory.CreateClient();
        _factory.Dispatcher.SendAsync(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Team));
    }

    [Fact]
    public async Task Post_WithoutIdempotencyKey_ShouldReturn400AndNotExecute()
    {
        var response = await _client.PostAsJsonAsync("/api/teams", TeamBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Idempotency.KeyRequired", await response.Content.ReadAsStringAsync());
        await _factory.Dispatcher.DidNotReceive().SendAsync(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_WithKeyLongerThan100Characters_ShouldReturn400()
    {
        var response = await PostAsync("/api/teams", new string('k', 101), TeamBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Idempotency.InvalidKey", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_RepeatedWithSameKeyAndBody_ShouldReturnOriginalResponseWithoutDuplicating()
    {
        var first = await PostAsync("/api/teams", "same-key", TeamBody);
        var second = await PostAsync("/api/teams", "same-key", TeamBody);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
        Assert.Equal(first.Headers.Location, second.Headers.Location);
        Assert.True(second.Headers.Contains("Idempotency-Replayed"));
        Assert.False(first.Headers.Contains("Idempotency-Replayed"));

        // El caso de uso se ejecutó UNA sola vez: no hay registros duplicados
        await _factory.Dispatcher.Received(1).SendAsync(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_RepeatedKeyWithDifferentBody_ShouldReturn409()
    {
        await PostAsync("/api/teams", "reused-key", TeamBody);

        var response = await PostAsync("/api/teams", "reused-key", new { fullName = "Otro Club", shortName = "OTR" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("Idempotency.KeyReused", await response.Content.ReadAsStringAsync());
        await _factory.Dispatcher.Received(1).SendAsync(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_SameKeyOnDifferentEndpoint_ShouldReturn409()
    {
        await PostAsync("/api/teams", "cross-endpoint", TeamBody);

        var response = await PostAsync($"/api/teams/{Team.Id}/players", "cross-endpoint",
            new { lastName = "Ventura", jerseyNumber = 1, position = "goalkeeper" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_WhileSameKeyIsInProgress_ShouldReturn409()
    {
        await PostAsync("/api/teams", "probe", TeamBody);
        _factory.IdempotencyStore.SeedInProgress("in-flight", _factory.IdempotencyStore.GetHash("probe")!);

        var response = await PostAsync("/api/teams", "in-flight", TeamBody);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("Idempotency.RequestInProgress", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_BusinessError_ShouldAlsoBeReplayedIdentically()
    {
        _factory.Dispatcher.SendAsync(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TeamResponse>(TeamErrors.FullNameAlreadyExists));

        var first = await PostAsync("/api/teams", "conflict-key", TeamBody);
        var second = await PostAsync("/api/teams", "conflict-key", TeamBody);

        Assert.Equal(HttpStatusCode.Conflict, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
        await _factory.Dispatcher.Received(1).SendAsync(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_UnexpectedFailure_ShouldReleaseKeySoClientCanRetry()
    {
        _factory.Dispatcher.SendAsync(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("falla transitoria"));

        var failed = await PostAsync("/api/teams", "retry-key", TeamBody);

        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        Assert.False(_factory.IdempotencyStore.Contains("retry-key"));

        _factory.Dispatcher.SendAsync(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Team));
        var retried = await PostAsync("/api/teams", "retry-key", TeamBody);

        Assert.Equal(HttpStatusCode.Created, retried.StatusCode);
    }

    [Fact]
    public async Task PostMatchResult_ShouldBeIdempotentAndReturn201()
    {
        var matchId = Guid.NewGuid();
        _factory.Dispatcher.SendAsync(Arg.Any<RegisterMatchResultCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new MatchResultResponse(matchId, MatchStatus.Finished, 2, 1, DateTime.UtcNow)));
        var body = new
        {
            homeGoals = 2,
            awayGoals = 1,
            goals = new[]
            {
                new { playerId = Guid.NewGuid(), teamId = Guid.NewGuid(), minute = 10 }
            }
        };

        var first = await PostAsync($"/api/matches/{matchId}/result", "result-key", body);
        var second = await PostAsync($"/api/matches/{matchId}/result", "result-key", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal($"/api/matches/{matchId}", first.Headers.Location?.AbsolutePath, ignoreCase: true);
        Assert.Equal(first.Headers.Location, second.Headers.Location);
        Assert.Contains("\"status\":\"finished\"", await first.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        await _factory.Dispatcher.Received(1).SendAsync(Arg.Any<RegisterMatchResultCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchMatch_ShouldDelegateRuleToUseCaseAndMapDomainError()
    {
        var matchId = Guid.NewGuid();
        _factory.Dispatcher.SendAsync(Arg.Any<ChangeMatchStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(MatchErrors.UnsupportedStatusChange));

        var response = await _client.PatchAsJsonAsync($"/api/matches/{matchId}", new { status = "finished" });

        // El controller solo orquesta: la regla vive en el dominio y llega como Result
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Match.UnsupportedStatusChange", await response.Content.ReadAsStringAsync());
        await _factory.Dispatcher.Received(1).SendAsync(
            Arg.Is<ChangeMatchStatusCommand>(command => command.MatchId == matchId && command.Status == MatchStatus.Finished),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchMatch_Cancel_ShouldReturn204()
    {
        _factory.Dispatcher.SendAsync(Arg.Any<ChangeMatchStatusCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var response = await _client.PatchAsJsonAsync($"/api/matches/{Guid.NewGuid()}", new { status = "cancelled" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostAsync(string url, string idempotencyKey, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return _client.SendAsync(request);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
