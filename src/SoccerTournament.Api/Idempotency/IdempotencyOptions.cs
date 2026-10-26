namespace SoccerTournament.Api.Idempotency;

/// <summary>
/// Configuración de la sección "Idempotency" de appsettings.
/// </summary>
public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    /// <summary>
    /// Tiempo durante el cual una Idempotency-Key devuelve la respuesta original.
    /// </summary>
    public int ExpirationHours { get; set; } = 24;
}
