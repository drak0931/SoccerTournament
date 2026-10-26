using System.Diagnostics;
using SoccerTournament.Application.Abstractions.Diagnostics;

namespace SoccerTournament.Infrastructure.Diagnostics;

/// <summary>
/// Fuente de spans de Infrastructure (Unit of Work, eventos de dominio, inicialización de BD).
/// </summary>
public static class InfrastructureDiagnostics
{
    public const string ActivitySourceName = "SoccerTournament.Infrastructure";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>
    /// Inicia una traza raíz para trabajo fuera de un request HTTP (arranque, seed). Si no hay un listener
    /// (OpenTelemetry deshabilitado) crea igualmente la Activity, para que logs y domain_events tengan TraceId.
    /// </summary>
    public static Activity StartRootActivity(string name, string correlationId)
    {
        // Sin Activity ambiente, tanto ActivitySource como new Activity() inician una traza nueva (raíz)
        Activity.Current = null;

        var activity = ActivitySource.StartActivity(name, ActivityKind.Internal)
                       ?? new Activity(name).SetIdFormat(ActivityIdFormat.W3C).Start();

        activity.SetBaggage(CorrelationContext.BaggageKey, correlationId);
        activity.SetTag(CorrelationContext.BaggageKey, correlationId);

        return activity;
    }
}
