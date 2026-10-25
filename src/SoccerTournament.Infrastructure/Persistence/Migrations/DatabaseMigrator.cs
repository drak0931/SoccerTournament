using DbUp;
using DbUp.Engine.Output;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Infrastructure.Persistence.Migrations;

/// <summary>
/// Aplica los scripts SQL versionados (Persistence/Scripts/*.sql) con DbUp.
/// Cada script se ejecuta una sola vez y queda registrado en dbo.schema_versions.
/// Crea la base de datos si no existe (necesario para el contenedor de Docker).
/// </summary>
internal sealed class DatabaseMigrator
{
    private const string ScriptsNamespaceMarker = ".Persistence.Scripts.";

    private readonly string _connectionString;
    private readonly DatabaseOptions _options;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(SqlConnectionStringProvider connectionStringProvider, IOptions<DatabaseOptions> options, ILogger<DatabaseMigrator> logger)
    {
        _connectionString = connectionStringProvider.ConnectionString;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result> MigrateAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return Migrate();
            }
            catch (SqlException exception) when (attempt < _options.StartupMaxRetries)
            {
                // Límite de infraestructura al arrancar: la BD aún no acepta conexiones
                _logger.LogWarning(
                    "No se pudo conectar a la BD (intento {Attempt}/{MaxRetries}, SqlError {SqlErrorNumber}). Reintentando en {DelaySeconds} s",
                    attempt, _options.StartupMaxRetries, exception.Number, _options.StartupRetryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(_options.StartupRetryDelaySeconds), cancellationToken);
            }
        }
    }

    private Result Migrate()
    {
        EnsureDatabase.For.SqlDatabase(_connectionString, new NoOpUpgradeLog());

        var upgrader = DeployChanges.To
            .SqlDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(AssemblyReference.Assembly, scriptName => scriptName.Contains(ScriptsNamespaceMarker))
            .JournalToSqlTable("dbo", "schema_versions")
            .WithTransactionPerScript()
            .LogToNowhere()
            .Build();

        var pendingScripts = upgrader.GetScriptsToExecute().Select(script => script.Name).ToArray();
        if (pendingScripts.Length == 0)
        {
            _logger.LogInformation("Esquema de BD actualizado: no hay scripts pendientes");
            return Result.Success();
        }

        _logger.LogInformation("Aplicando {ScriptCount} scripts de BD: {Scripts}", pendingScripts.Length, pendingScripts);

        var upgradeResult = upgrader.PerformUpgrade();
        if (!upgradeResult.Successful)
        {
            _logger.LogError(
                upgradeResult.Error,
                "Falló el script de BD {ScriptName}; la transacción del script se revirtió",
                upgradeResult.ErrorScript?.Name);

            return Error.Failure($"Falló la actualización del esquema en el script '{upgradeResult.ErrorScript?.Name}'.", "Database.MigrationFailed");
        }

        _logger.LogInformation("Scripts de BD aplicados correctamente: {ScriptCount}", upgradeResult.Scripts.Count());
        return Result.Success();
    }
}
