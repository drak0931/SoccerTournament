namespace SoccerTournament.Domain.Abstractions;

/// <summary>
/// Agrupa varios errores de validación en un único error (se traduce a 400 con el detalle de cada campo).
/// </summary>
public sealed record ValidationError : Error
{
    public ValidationError(IReadOnlyCollection<Error> errors)
        : base("Uno o más datos de entrada no son válidos.", ErrorType.Validation, "Validation.Failed")
    {
        Errors = errors;
    }

    public IReadOnlyCollection<Error> Errors { get; }
}
