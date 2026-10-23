namespace SoccerTournament.Domain.Abstractions;

/// <summary>
/// Describe un error de negocio. Se transporta dentro de <see cref="Result"/> en lugar de lanzar excepciones.
/// </summary>
/// <param name="Message">Mensaje legible para el consumidor de la API.</param>
/// <param name="Type">Categoría del error, usada para mapear el estado HTTP.</param>
/// <param name="Code">Código opcional y estable para que el cliente identifique el error (ej. "Team.NotFound").</param>
public record Error(string Message, ErrorType Type, string? Code = null)
{
    public static readonly Error None = new(string.Empty, ErrorType.None);

    public static Error Failure(string message, string? code = null) => new(message, ErrorType.Failure, code);

    public static Error Validation(string message, string? code = null) => new(message, ErrorType.Validation, code);

    public static Error NotFound(string message, string? code = null) => new(message, ErrorType.NotFound, code);

    public static Error Conflict(string message, string? code = null) => new(message, ErrorType.Conflict, code);
}
