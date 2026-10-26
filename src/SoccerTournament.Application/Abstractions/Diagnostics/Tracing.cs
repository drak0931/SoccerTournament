using System.Diagnostics;

namespace SoccerTournament.Application.Abstractions.Diagnostics;

/// <summary>
/// Fuente de spans de la capa Application (commands y queries). Usa System.Diagnostics del BCL:
/// Application no depende de OpenTelemetry; la Api decide si exporta los spans.
/// </summary>
public static class ApplicationDiagnostics
{
    public const string ActivitySourceName = "SoccerTournament.Application";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}

/// <summary>
/// Convenciones de correlación compartidas por Api (middleware) e Infrastructure (domain_events).
/// </summary>
public static class CorrelationContext
{
    public const string HeaderName = "X-Correlation-Id";
    public const string TraceIdHeaderName = "X-Trace-Id";

    /// <summary>
    /// Clave del baggage de <see cref="Activity"/> que viaja con el request y llega a domain_events.correlation_id.
    /// </summary>
    public const string BaggageKey = "correlation.id";

    public const string LogPropertyName = "CorrelationId";

    public static string? Current => Activity.Current?.GetBaggageItem(BaggageKey);
}
