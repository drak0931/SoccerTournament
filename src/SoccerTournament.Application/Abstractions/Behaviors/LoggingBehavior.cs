using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SoccerTournament.Application.Abstractions.Diagnostics;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Abstractions.Behaviors;

/// <summary>
/// Observabilidad de cada caso de uso: abre un span ("Command X" / "Query Y") y registra con logs
/// estructurados el inicio, fin, duración y resultado. No registra el contenido del request
/// para evitar exponer datos sensibles.
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResult> :
    ICommandPipelineBehavior<TRequest, TResult>,
    IQueryPipelineBehavior<TRequest, TResult>
    where TResult : Result
{
    private readonly ILogger<LoggingBehavior<TRequest, TResult>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<TResult> HandleAsync(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestKind = request is ICommandBase ? "Command" : "Query";

        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity($"{requestKind} {requestName}");
        activity?.SetTag("app.request.kind", requestKind);
        activity?.SetTag("app.request.name", requestName);

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestName"] = requestName,
            ["RequestKind"] = requestKind
        });

        _logger.LogDebug("Procesando {RequestKind} {RequestName}", requestKind, requestName);

        var stopwatch = Stopwatch.StartNew();
        var result = await next();
        stopwatch.Stop();

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "{RequestKind} {RequestName} completado en {ElapsedMilliseconds} ms",
                requestKind, requestName, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            // Un fallo de negocio no es un error del sistema: se registra como Warning y el span no se marca como error
            activity?.SetTag("app.error.type", result.Error.Type.ToString());
            activity?.SetTag("app.error.code", result.Error.Code);

            _logger.LogWarning(
                "{RequestKind} {RequestName} finalizó con error {ErrorType} {ErrorCode} en {ElapsedMilliseconds} ms",
                requestKind, requestName, result.Error.Type, result.Error.Code, stopwatch.ElapsedMilliseconds);
        }

        return result;
    }
}
