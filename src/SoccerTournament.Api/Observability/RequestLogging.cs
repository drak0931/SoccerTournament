using Serilog;
using Serilog.AspNetCore;
using Serilog.Events;

namespace SoccerTournament.Api.Observability;

/// <summary>
/// Un log por request con su duración: "HTTP {Method} {Path} respondió {StatusCode} en {Elapsed} ms".
/// No incluye query string, cuerpo, headers ni IP del cliente.
/// </summary>
public static class RequestLogging
{
    public const string MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0.0} ms";

    public static void Configure(RequestLoggingOptions options)
    {
        options.MessageTemplate = MessageTemplate;
        options.GetLevel = GetLevel;
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var endpoint = httpContext.GetEndpoint()?.DisplayName;
            if (endpoint is not null)
            {
                diagnosticContext.Set("Endpoint", endpoint);
            }
        };
    }

    /// <summary>
    /// 5xx o excepción → Error · 4xx → Warning · health checks → Debug · resto → Information.
    /// </summary>
    public static LogEventLevel GetLevel(HttpContext httpContext, double elapsedMilliseconds, Exception? exception)
    {
        if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        if (httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            return LogEventLevel.Warning;
        }

        return httpContext.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Debug : LogEventLevel.Information;
    }
}
