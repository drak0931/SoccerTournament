using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace SoccerTournament.Api.Observability;

/// <summary>
/// Agrega TraceId y SpanId (W3C) a cada log para enlazarlo con la traza en Jaeger y con domain_events.trace_id.
/// </summary>
public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));
    }
}
