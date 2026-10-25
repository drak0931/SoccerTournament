using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoccerTournament.Infrastructure.Persistence;
using SoccerTournament.Infrastructure.Persistence.Migrations;
using SoccerTournament.Infrastructure.Persistence.Seeding;

namespace SoccerTournament.Infrastructure;

/// <summary>
/// Prepara la BD al iniciar la API: aplica scripts pendientes y, si la BD está vacía, carga el seed de datos.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        if (!options.RunMigrationsOnStartup)
        {
            logger.LogInformation("Inicialización de BD deshabilitada (Database:RunMigrationsOnStartup = false)");
            return;
        }

        var migration = await provider.GetRequiredService<DatabaseMigrator>().MigrateAsync(cancellationToken);
        if (migration.IsFailure)
        {
            // Sin esquema válido la API no puede atender requests: se detiene el arranque
            throw new InvalidOperationException(migration.ErrorMessage);
        }

        if (!options.SeedOnStartup)
        {
            logger.LogInformation("Seed de datos deshabilitado (Database:SeedOnStartup = false)");
            return;
        }

        // Un seed fallido no impide operar la API: el error queda registrado y se reintenta en el próximo arranque
        await provider.GetRequiredService<DatabaseSeeder>().SeedAsync(cancellationToken);
    }
}
