using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Api.Common;

/// <summary>
/// Base de los controllers: solo orquestan (request → command/query → IDispatcher → Result → HTTP).
/// Centraliza el mapeo de Result a códigos HTTP y ProblemDetails.
/// </summary>
// Sin [Produces]: ese filtro reemplaza el Content-Type y convertiría application/problem+json en application/json
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ApiControllerBase(IDispatcher dispatcher)
    {
        Dispatcher = dispatcher;
    }

    protected IDispatcher Dispatcher { get; }

    /// <summary>
    /// 200 OK con el valor, o el error correspondiente.
    /// </summary>
    protected IActionResult OkOrProblem<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : Problem(result.Error);

    /// <summary>
    /// 201 Created con header Location apuntando al recurso, o el error correspondiente.
    /// </summary>
    protected IActionResult CreatedOrProblem<T>(Result<T> result, string actionName, string? controllerName, Func<T, object> routeValues) =>
        result.IsSuccess
            ? CreatedAtAction(actionName, controllerName, routeValues(result.Value), result.Value)
            : Problem(result.Error);

    /// <summary>
    /// 204 No Content, o el error correspondiente.
    /// </summary>
    protected IActionResult NoContentOrProblem(Result result) =>
        result.IsSuccess ? NoContent() : Problem(result.Error);

    /// <summary>
    /// Traduce un error de negocio a ProblemDetails: Validation → 400, NotFound → 404, Conflict → 409, Failure → 500.
    /// </summary>
    protected IActionResult Problem(Error error)
    {
        var statusCode = ErrorHttpMapping.ToStatusCode(error.Type);

        var problem = ProblemDetailsFactory.CreateProblemDetails(
            HttpContext,
            statusCode,
            title: ErrorHttpMapping.ToTitle(error.Type),
            detail: error.Message);

        if (error.Code is not null)
        {
            problem.Extensions["errorCode"] = error.Code;
        }

        if (error is ValidationError validationError)
        {
            problem.Extensions["errors"] = validationError.Errors
                .GroupBy(item => ToFieldName(item.Code))
                .ToDictionary(group => group.Key, group => group.Select(item => item.Message).ToArray());
        }

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    // Los nombres de propiedad de FluentValidation se exponen en camelCase, igual que el JSON de la API
    private static string ToFieldName(string? code) =>
        string.IsNullOrWhiteSpace(code) ? "general" : code.Contains('.') ? code : JsonNamingPolicy.CamelCase.ConvertName(code);
}
