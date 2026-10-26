using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace SoccerTournament.Api.Tests.Infrastructure;

/// <summary>
/// Sink de Serilog en memoria para verificar los logs estructurados que produce la API.
/// </summary>
public sealed class CollectingLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    public IReadOnlyCollection<LogEvent> Events => _events.ToArray();

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);

    /// <summary>
    /// El log del request se escribe al terminar el pipeline; se espera brevemente a que aparezca.
    /// </summary>
    public async Task<LogEvent> WaitForAsync(Func<LogEvent, bool> predicate, int timeoutMilliseconds = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);

        while (DateTime.UtcNow < deadline)
        {
            var match = _events.FirstOrDefault(predicate);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException("No se registró el log esperado.");
    }
}
