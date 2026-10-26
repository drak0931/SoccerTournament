using System.Diagnostics;
using System.Text.RegularExpressions;
using Serilog.Context;
using SoccerTournament.Application.Abstractions.Diagnostics;

namespace SoccerTournament.Api.Observability;

/// <summary>
/// Asigna a cada request un CorrelationId y expone el TraceId:
/// <list type="bullet">
/// <item>Respeta X-Correlation-Id del cliente si es válido; si no, genera uno nuevo.</item>
/// <item>Devuelve X-Correlation-Id y X-Trace-Id en la respuesta.</item>
/// <item>Propaga el CorrelationId como baggage de la Activity (llega a domain_events) y al LogContext de Serilog.</item>
/// </list>
/// </summary>
public sealed partial class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[CorrelationContext.HeaderName].ToString());
        var activity = Activity.Current;

        activity?.SetBaggage(CorrelationContext.BaggageKey, correlationId);
        activity?.SetTag(CorrelationContext.BaggageKey, correlationId);

        // OnStarting: el manejador de excepciones limpia los headers antes de escribir el 500;
        // así X-Correlation-Id y X-Trace-Id se conservan justamente en las respuestas de error
        var traceId = activity?.TraceId.ToString();
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationContext.HeaderName] = correlationId;
            if (traceId is not null)
            {
                context.Response.Headers[CorrelationContext.TraceIdHeaderName] = traceId;
            }

            return Task.CompletedTask;
        });

        // Todos los logs emitidos durante el request incluyen CorrelationId
        using (LogContext.PushProperty(CorrelationContext.LogPropertyName, correlationId))
        {
            await _next(context);
        }
    }

    // Solo se acepta un valor seguro (evita inyección en logs y headers); si no, se genera uno nuevo
    internal static string ResolveCorrelationId(string? headerValue) =>
        !string.IsNullOrWhiteSpace(headerValue) && SafeCorrelationId().IsMatch(headerValue)
            ? headerValue
            : Guid.NewGuid().ToString("N");

    [GeneratedRegex(@"^[A-Za-z0-9\-_.:]{1,64}$")]
    private static partial Regex SafeCorrelationId();
}
