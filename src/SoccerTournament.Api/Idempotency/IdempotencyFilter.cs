using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using SoccerTournament.Application.Abstractions.Clock;
using SoccerTournament.Application.Abstractions.Idempotency;

namespace SoccerTournament.Api.Idempotency;

/// <summary>
/// Idempotencia para POST basada en el header Idempotency-Key:
/// <list type="bullet">
/// <item>Sin header → 400.</item>
/// <item>Clave nueva → se reclama, se ejecuta la acción y se guarda la respuesta (status, body, Location).</item>
/// <item>Misma clave y mismo payload → se devuelve la respuesta ORIGINAL sin volver a ejecutar (sin duplicados).</item>
/// <item>Misma clave con otro payload, o todavía en proceso → 409.</item>
/// <item>Error inesperado (5xx) → se libera la clave para permitir reintentos.</item>
/// </list>
/// </summary>
public sealed partial class IdempotencyFilter : IAsyncActionFilter
{
    private readonly IIdempotencyStore _store;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IdempotencyOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly LinkGenerator _linkGenerator;
    private readonly ILogger<IdempotencyFilter> _logger;

    public IdempotencyFilter(
        IIdempotencyStore store,
        IDateTimeProvider dateTimeProvider,
        IOptions<IdempotencyOptions> options,
        IOptions<JsonOptions> jsonOptions,
        ProblemDetailsFactory problemDetailsFactory,
        LinkGenerator linkGenerator,
        ILogger<IdempotencyFilter> logger)
    {
        _store = store;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        _problemDetailsFactory = problemDetailsFactory;
        _linkGenerator = linkGenerator;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var idempotencyKey = httpContext.Request.Headers[IdempotentAttribute.HeaderName].ToString().Trim();

        if (idempotencyKey.Length == 0)
        {
            context.Result = Problem(httpContext, StatusCodes.Status400BadRequest,
                $"El header {IdempotentAttribute.HeaderName} es obligatorio en esta operación.", "Idempotency.KeyRequired");
            return;
        }

        if (!ValidKeyPattern().IsMatch(idempotencyKey))
        {
            context.Result = Problem(httpContext, StatusCodes.Status400BadRequest,
                $"El header {IdempotentAttribute.HeaderName} debe tener entre 1 y 100 caracteres imprimibles sin espacios.", "Idempotency.InvalidKey");
            return;
        }

        var request = new IdempotencyRequest(
            idempotencyKey,
            httpContext.Request.Method,
            httpContext.Request.Path.Value ?? string.Empty,
            ComputeRequestHash(context));

        var claim = await _store.TryClaimAsync(
            request,
            _dateTimeProvider.UtcNow.AddHours(_options.ExpirationHours),
            httpContext.RequestAborted);

        if (!claim.Claimed)
        {
            context.Result = HandleExistingKey(httpContext, request, claim.Existing!);
            return;
        }

        ActionExecutedContext executed;
        try
        {
            executed = await next();
        }
        catch
        {
            // Límite HTTP: se libera la clave y la excepción continúa hacia el manejador global (500)
            await _store.ReleaseAsync(idempotencyKey, CancellationToken.None);
            throw;
        }

        var response = executed.Exception is null || executed.ExceptionHandled
            ? CaptureResponse(executed)
            : null;

        if (response is null || response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            await _store.ReleaseAsync(idempotencyKey, CancellationToken.None);
            return;
        }

        // Se guarda aunque la respuesta sea 4xx: repetir el mismo request debe dar la misma respuesta
        await _store.CompleteAsync(idempotencyKey, response, CancellationToken.None);
    }

    private IActionResult HandleExistingKey(HttpContext httpContext, IdempotencyRequest request, IdempotencyRecord existing)
    {
        if (!string.Equals(existing.RequestHash, request.RequestHash, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Idempotency-Key reutilizada con un payload distinto en {RequestPath}", request.Path);
            return Problem(httpContext, StatusCodes.Status409Conflict,
                "La Idempotency-Key ya se utilizó con una solicitud diferente. Genere una clave nueva.", "Idempotency.KeyReused");
        }

        if (existing.Status == IdempotencyStatus.InProgress || existing.Response is null)
        {
            _logger.LogWarning("Solicitud con Idempotency-Key aún en proceso en {RequestPath}", request.Path);
            return Problem(httpContext, StatusCodes.Status409Conflict,
                "Ya se está procesando una solicitud con esta Idempotency-Key. Intente de nuevo en unos segundos.", "Idempotency.RequestInProgress");
        }

        _logger.LogInformation(
            "Solicitud repetida con Idempotency-Key en {RequestPath}: se devuelve la respuesta original {StatusCode}",
            request.Path, existing.Response.StatusCode);

        httpContext.Response.Headers[IdempotentAttribute.ReplayedHeaderName] = "true";
        if (!string.IsNullOrEmpty(existing.Response.Location))
        {
            httpContext.Response.Headers.Location = existing.Response.Location;
        }

        return new ContentResult
        {
            StatusCode = existing.Response.StatusCode,
            Content = existing.Response.Body,
            ContentType = existing.Response.StatusCode >= StatusCodes.Status400BadRequest
                ? "application/problem+json"
                : "application/json"
        };
    }

    // Captura la respuesta antes de que se escriba: status, body serializado y Location
    private IdempotencyResponse? CaptureResponse(ActionExecutedContext executed)
    {
        switch (executed.Result)
        {
            case ObjectResult objectResult:
                var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
                var body = objectResult.Value is null ? null : JsonSerializer.Serialize(objectResult.Value, objectResult.Value.GetType(), _jsonOptions);
                return new IdempotencyResponse(statusCode, body, ResolveLocation(executed));

            case IStatusCodeActionResult statusCodeResult:
                return new IdempotencyResponse(statusCodeResult.StatusCode ?? StatusCodes.Status200OK, null, null);

            default:
                return null;
        }
    }

    // Misma URL absoluta que genera CreatedAtActionResult, para que la repetición sea idéntica a la original
    private string? ResolveLocation(ActionExecutedContext executed) => executed.Result switch
    {
        CreatedAtActionResult created => _linkGenerator.GetUriByAction(
            executed.HttpContext,
            created.ActionName,
            created.ControllerName ?? executed.RouteData.Values["controller"]?.ToString(),
            created.RouteValues),
        CreatedResult created => created.Location,
        _ => null
    };

    // Huella del request: método + ruta + argumentos ya enlazados (body y ruta), en orden estable
    private string ComputeRequestHash(ActionExecutingContext context)
    {
        var arguments = context.ActionArguments
            .Where(argument => argument.Value is not CancellationToken)
            .OrderBy(argument => argument.Key, StringComparer.Ordinal)
            .ToDictionary(argument => argument.Key, argument => argument.Value);

        var fingerprint = string.Join('\n',
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path.Value?.ToLowerInvariant(),
            JsonSerializer.Serialize(arguments, _jsonOptions));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint))).ToLowerInvariant();
    }

    private ObjectResult Problem(HttpContext httpContext, int statusCode, string detail, string errorCode)
    {
        var problem = _problemDetailsFactory.CreateProblemDetails(httpContext, statusCode, title: "Idempotencia", detail: detail);
        problem.Extensions["errorCode"] = errorCode;

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    // ASCII imprimible sin espacios, 1 a 100 caracteres (longitud de dbo.idempotency_records.idempotency_key)
    [GeneratedRegex(@"^[\x21-\x7E]{1,100}$")]
    private static partial Regex ValidKeyPattern();
}
