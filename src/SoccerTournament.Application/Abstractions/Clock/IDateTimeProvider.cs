namespace SoccerTournament.Application.Abstractions.Clock;

/// <summary>
/// Fuente de la hora actual en UTC. Se abstrae para que los casos de uso sean deterministas en pruebas.
/// La conversión a America/El_Salvador se realiza en la capa de presentación.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
