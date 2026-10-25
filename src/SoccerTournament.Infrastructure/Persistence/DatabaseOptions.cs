namespace SoccerTournament.Infrastructure.Persistence;

/// <summary>
/// Configuración de la sección "Database" de appsettings.
/// La cadena de conexión NO va aquí: se lee de ConnectionStrings:SoccerTournament
/// (user-secrets en desarrollo, variable de entorno en Docker).
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "SoccerTournament";

    /// <summary>
    /// Ejecuta los scripts DbUp pendientes al iniciar la API.
    /// </summary>
    public bool RunMigrationsOnStartup { get; set; } = true;

    /// <summary>
    /// Reintentos de conexión al iniciar (la BD del contenedor puede tardar en estar lista).
    /// </summary>
    public int StartupMaxRetries { get; set; } = 10;

    public int StartupRetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Reintentos automáticos de EF Core ante errores transitorios.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    public int CommandTimeoutSeconds { get; set; } = 30;
}
