using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SoccerTournament.Application.Abstractions.Clock;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Infrastructure.Clock;
using SoccerTournament.Infrastructure.Persistence;
using SoccerTournament.Infrastructure.Persistence.Auditing;
using SoccerTournament.Infrastructure.Persistence.DomainEvents;
using SoccerTournament.Infrastructure.Persistence.Migrations;
using SoccerTournament.Infrastructure.Persistence.Repositories;
using SoccerTournament.Infrastructure.ReadSide;

namespace SoccerTournament.Infrastructure;

/// <summary>
/// Registro de dependencias de Infrastructure: implementa los contratos definidos en Application.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Error de configuración: se detiene el arranque con un mensaje claro
            throw new InvalidOperationException(
                $"Falta la cadena de conexión 'ConnectionStrings:{DatabaseOptions.ConnectionStringName}'. " +
                "Configúrela con user-secrets (desarrollo) o variable de entorno (Docker).");
        }

        EnsureValidConnectionString(connectionString);

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddSingleton(new SqlConnectionStringProvider(connectionString));
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        AddWriteSide(services, connectionString);
        AddReadSide(services);

        services.AddTransient<DatabaseMigrator>();

        return services;
    }

    // Valida el formato al arrancar. El mensaje NO incluye la cadena para no exponer credenciales en logs
    private static void EnsureValidConnectionString(string connectionString)
    {
        try
        {
            _ = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"La cadena de conexión 'ConnectionStrings:{DatabaseOptions.ConnectionStringName}' no tiene un formato válido: {exception.Message} " +
                "Revise que esté en una sola línea y sin caracteres extra.",
                exception);
        }
    }

    // Lado de lectura: Dapper sobre conexiones ADO.NET propias (sin DbContext)
    private static void AddReadSide(IServiceCollection services)
    {
        services.AddSingleton<SqlConnectionFactory>();

        services.AddScoped<ITeamReadService, TeamReadService>();
        services.AddScoped<IPlayerReadService, PlayerReadService>();
        services.AddScoped<IMatchReadService, MatchReadService>();
        services.AddScoped<IStandingReadService, StandingReadService>();
        services.AddScoped<IScorerReadService, ScorerReadService>();
    }

    // Lado de escritura: EF Core + Unit of Work + repositorios
    private static void AddWriteSide(IServiceCollection services, string connectionString)
    {
        services.AddSingleton<AuditInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseSqlServer(connectionString, sqlServer =>
            {
                sqlServer.EnableRetryOnFailure(databaseOptions.MaxRetryCount);
                sqlServer.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
            });

            options.AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DomainEventDispatcher>();

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IStandingRepository, StandingRepository>();
    }
}
