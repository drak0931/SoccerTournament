using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoccerTournament.Api.Tests.Infrastructure;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Features.Players.Queries;
using SoccerTournament.Application.Features.Teams.Commands;
using SoccerTournament.Application.Features.Teams.Queries;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Api.Tests;

/// <summary>
/// Verbos REST, mapeo Result → HTTP y ProblemDetails sobre el recurso equipos.
/// </summary>
public sealed class TeamsEndpointsTests : IDisposable
{
    private static readonly Guid TeamId = Guid.Parse("8f1c2b1e-3d4a-4f5b-9c6d-7e8f9a0b1c2d");
    private static readonly TeamResponse Team = new(TeamId, "Club Deportivo Volcanes", "VOL", new DateTime(1925, 3, 15));

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public TeamsEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetTeams_ShouldReturnStandardPagedEnvelope()
    {
        _factory.Dispatcher.QueryAsync(Arg.Any<GetTeamsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PagedResult<TeamResponse>(new[] { Team }, 1, 10, 50)));

        var response = await _client.GetAsync("/api/teams?pageNumber=1&pageSize=10&sortBy=fullName&sortDirection=desc&name=vol");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.Equal(1, json.RootElement.GetProperty("pageNumber").GetInt32());
        Assert.Equal(10, json.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(50, json.RootElement.GetProperty("totalRecords").GetInt32());
        Assert.Equal(5, json.RootElement.GetProperty("totalPages").GetInt32());

        await _factory.Dispatcher.Received(1).QueryAsync(
            Arg.Is<GetTeamsQuery>(query => query.SortBy == "fullName" && query.SortDirection == SortDirection.Desc && query.Name == "vol"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTeams_WithInvalidSortDirection_ShouldReturn400()
    {
        var response = await _client.GetAsync("/api/teams?sortDirection=sideways");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTeamById_WhenMissing_ShouldReturn404ProblemWithCodeAndTraceId()
    {
        _factory.Dispatcher.QueryAsync(Arg.Any<GetTeamByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TeamResponse>(TeamErrors.NotFound(TeamId)));

        var response = await _client.GetAsync($"/api/teams/{TeamId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Team.NotFound", json.RootElement.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task PostTeam_ShouldReturn201WithLocation()
    {
        GivenCreateTeamReturns(Result.Success(Team));

        var response = await PostTeamAsync("key-201", new { fullName = Team.FullName, shortName = Team.ShortName });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/teams/{TeamId}", response.Headers.Location?.AbsolutePath, ignoreCase: true);
    }

    [Fact]
    public async Task PostTeam_WithValidationError_ShouldReturn400WithFieldErrors()
    {
        var validation = new ValidationError(new[] { Error.Validation("'Full Name' no debería estar vacío.", "FullName") });
        GivenCreateTeamReturns(Result.Failure<TeamResponse>(validation));

        var response = await PostTeamAsync("key-400", new { fullName = "", shortName = "VOL" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("fullName", out _));
    }

    [Fact]
    public async Task PostTeam_WithDuplicatedName_ShouldReturn409()
    {
        GivenCreateTeamReturns(Result.Failure<TeamResponse>(TeamErrors.FullNameAlreadyExists));

        var response = await PostTeamAsync("key-409", new { fullName = Team.FullName, shortName = "XYZ" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Equal("Team.FullNameAlreadyExists", json.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task PutTeam_ShouldReturn200WithReplacedResource()
    {
        _factory.Dispatcher.SendAsync(Arg.Any<UpdateTeamCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Team));

        var response = await _client.PutAsJsonAsync($"/api/teams/{TeamId}", new { fullName = Team.FullName, shortName = "VOL", foundedYear = "1925-03-15" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _factory.Dispatcher.Received(1).SendAsync(
            Arg.Is<UpdateTeamCommand>(command => command.TeamId == TeamId && command.ShortName == "VOL"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchTeam_ShouldOnlySendProvidedFields()
    {
        _factory.Dispatcher.SendAsync(Arg.Any<PatchTeamCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Team));

        var response = await PatchAsync($"/api/teams/{TeamId}", """{ "shortName": "VLC", "foundedYear": null }""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _factory.Dispatcher.Received(1).SendAsync(
            Arg.Is<PatchTeamCommand>(command =>
                !command.FullName.HasValue
                && command.ShortName.HasValue && command.ShortName.Value == "VLC"
                && command.FoundedYear.HasValue && command.FoundedYear.Value == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchTeam_WithUnknownField_ShouldReturn400WithoutCallingUseCase()
    {
        var response = await PatchAsync($"/api/teams/{TeamId}", """{ "stadium": "Cuscatlán" }""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _factory.Dispatcher.DidNotReceive().SendAsync(Arg.Any<PatchTeamCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTeam_ShouldReturn204EvenWhenRepeated()
    {
        _factory.Dispatcher.SendAsync(Arg.Any<DeleteTeamCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var first = await _client.DeleteAsync($"/api/teams/{TeamId}");
        var second = await _client.DeleteAsync($"/api/teams/{TeamId}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task DeleteTeam_WithDependencies_ShouldReturn409()
    {
        _factory.Dispatcher.SendAsync(Arg.Any<DeleteTeamCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure(TeamErrors.HasDependencies));

        var response = await _client.DeleteAsync($"/api/teams/{TeamId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetTeamPlayers_ForMissingTeam_ShouldReturn404()
    {
        _factory.Dispatcher.QueryAsync(Arg.Any<GetTeamPlayersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PagedResult<PlayerResponse>>(TeamErrors.NotFound(TeamId)));

        var response = await _client.GetAsync($"/api/teams/{TeamId}/players?sortBy=jerseyNumber");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await _factory.Dispatcher.Received(1).QueryAsync(
            Arg.Is<GetTeamPlayersQuery>(query => query.TeamId == TeamId && query.SortBy == "jerseyNumber" && query.SortDirection == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Health_WhenDatabaseIsUnreachable_ShouldReportUnhealthyWithoutDetails()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Unhealthy", body);
    }

    [Fact]
    public async Task UnexpectedException_ShouldReturn500ProblemWithoutInternalDetails()
    {
        _factory.Dispatcher.QueryAsync(Arg.Any<GetTeamByIdQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("detalle interno que no debe filtrarse"));

        var response = await _client.GetAsync($"/api/teams/{TeamId}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("detalle interno", body);
        Assert.Contains("traceId", body);
    }

    private void GivenCreateTeamReturns(Result<TeamResponse> result) =>
        _factory.Dispatcher.SendAsync(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>()).Returns(result);

    private Task<HttpResponseMessage> PostTeamAsync(string idempotencyKey, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams") { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> PatchAsync(string url, string json) =>
        _client.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/merge-patch+json"));

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
