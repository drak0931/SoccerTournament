using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog.Events;
using SoccerTournament.Api.Observability;
using SoccerTournament.Api.Tests.Infrastructure;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Features.Teams.Commands;
using SoccerTournament.Application.Features.Teams.Queries;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Api.Tests;

/// <summary>
/// Trazabilidad por request: CorrelationId, TraceId, duración y logs estructurados enlazados.
/// </summary>
public sealed class ObservabilityTests : IDisposable
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string TraceHeader = "X-Trace-Id";

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ObservabilityTests()
    {
        _client = _factory.CreateClient();
        _factory.Dispatcher.QueryAsync(Arg.Any<GetTeamsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PagedResult<TeamResponse>(Array.Empty<TeamResponse>(), 1, 10, 0)));
    }

    [Fact]
    public async Task Request_WithValidCorrelationId_ShouldEchoItBack()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/teams");
        request.Headers.Add(CorrelationHeader, "frontend-req-123");

        var response = await _client.SendAsync(request);

        Assert.Equal("frontend-req-123", Single(response, CorrelationHeader));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("valor con espacios")]
    [InlineData("inyeccion\r\nX-Falso: 1")]
    public async Task Request_WithoutOrUnsafeCorrelationId_ShouldGenerateANewOne(string? headerValue)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/teams");
        if (headerValue is not null)
        {
            request.Headers.TryAddWithoutValidation(CorrelationHeader, headerValue);
        }

        var response = await _client.SendAsync(request);

        var correlationId = Single(response, CorrelationHeader);
        Assert.Matches("^[0-9a-f]{32}$", correlationId);
    }

    [Fact]
    public async Task Response_ShouldExposeW3CTraceId()
    {
        var response = await _client.GetAsync("/api/teams");

        Assert.Matches("^[0-9a-f]{32}$", Single(response, TraceHeader));
    }

    [Fact]
    public async Task ProblemDetails_TraceId_ShouldMatchTraceHeader()
    {
        _factory.Dispatcher.QueryAsync(Arg.Any<GetTeamByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TeamResponse>(TeamErrors.NotFound(Guid.Empty)));

        var response = await _client.GetAsync($"/api/teams/{Guid.NewGuid()}");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(Single(response, TraceHeader), json.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task RequestLog_ShouldIncludeDurationTraceIdAndCorrelationId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/teams?name=secreto");
        request.Headers.Add(CorrelationHeader, "corr-log-001");

        var response = await _client.SendAsync(request);

        var log = await _factory.LogSink.WaitForAsync(IsRequestLog);
        Assert.Equal(LogEventLevel.Information, log.Level);
        Assert.Equal("\"/api/teams\"", log.Properties["RequestPath"].ToString());
        Assert.Equal("200", log.Properties["StatusCode"].ToString());
        Assert.True(log.Properties.ContainsKey("Elapsed"));
        Assert.Equal($"\"{Single(response, TraceHeader)}\"", log.Properties["TraceId"].ToString());
        Assert.Equal("\"corr-log-001\"", log.Properties["CorrelationId"].ToString());

        // Sin query string ni datos de entrada en el log
        Assert.DoesNotContain("secreto", log.RenderMessage());
    }

    [Fact]
    public async Task RequestLog_For4xx_ShouldBeWarning()
    {
        _factory.Dispatcher.QueryAsync(Arg.Any<GetTeamByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TeamResponse>(TeamErrors.NotFound(Guid.Empty)));

        await _client.GetAsync($"/api/teams/{Guid.NewGuid()}");

        var log = await _factory.LogSink.WaitForAsync(IsRequestLog);
        Assert.Equal(LogEventLevel.Warning, log.Level);
    }

    [Fact]
    public async Task UnexpectedError_ShouldBeLoggedAsErrorWithTraceId()
    {
        _factory.Dispatcher.QueryAsync(Arg.Any<GetTeamByIdQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("falla"));

        var response = await _client.GetAsync($"/api/teams/{Guid.NewGuid()}");

        var traceId = Single(response, TraceHeader);
        var errorLog = await _factory.LogSink.WaitForAsync(log => log.Level == LogEventLevel.Error && log.Exception is not null);
        Assert.Equal($"\"{traceId}\"", errorLog.Properties["TraceId"].ToString());

        var requestLog = await _factory.LogSink.WaitForAsync(IsRequestLog);
        Assert.Equal(LogEventLevel.Error, requestLog.Level);
    }

    [Fact]
    public async Task Logs_ShouldNotContainRequestBodies()
    {
        _factory.Dispatcher.SendAsync(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new TeamResponse(Guid.NewGuid(), "Club Dato Privado", "CDP", null)));
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { fullName = "Club Dato Privado", shortName = "CDP" })
        };
        request.Headers.Add("Idempotency-Key", "obs-body-001");

        var response = await _client.SendAsync(request);
        await _factory.LogSink.WaitForAsync(IsRequestLog);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.DoesNotContain(_factory.LogSink.Events, log => log.RenderMessage().Contains("Dato Privado"));
    }

    private static bool IsRequestLog(LogEvent log) =>
        log.MessageTemplate.Text == RequestLogging.MessageTemplate;

    private static string Single(HttpResponseMessage response, string header) =>
        Assert.Single(response.Headers.GetValues(header));

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
