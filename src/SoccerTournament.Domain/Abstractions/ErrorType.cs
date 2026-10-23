namespace SoccerTournament.Domain.Abstractions;

/// <summary>
/// Categoría del error. La Api la traduce al código HTTP correspondiente
/// (Validation → 400, NotFound → 404, Conflict → 409, Failure → 500).
/// </summary>
public enum ErrorType
{
    None = 0,
    Failure = 1,
    Validation = 2,
    NotFound = 3,
    Conflict = 4
}
