using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Api.Common;

/// <summary>
/// Correspondencia única entre el tipo de error de negocio y el código HTTP.
/// </summary>
public static class ErrorHttpMapping
{
    public static int ToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    public static string ToTitle(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "Datos de entrada no válidos",
        ErrorType.NotFound => "Recurso no encontrado",
        ErrorType.Conflict => "Conflicto con el estado actual del recurso",
        _ => "Error al procesar la solicitud"
    };
}
