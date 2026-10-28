using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SoccerTournament.Infrastructure.ReadSide;

/// <summary>
/// Verifica que SQL Server responde (SELECT 1). Lo usa /health, y con él el healthcheck de Docker.
/// No expone detalles de la conexión en la respuesta.
/// </summary>
internal sealed class DatabaseHealthCheck : IHealthCheck
{
    private const int TimeoutSeconds = 5;

    private readonly SqlConnectionFactory _connectionFactory;

    public DatabaseHealthCheck(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(timeout.Token);
            await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1", commandTimeout: TimeoutSeconds, cancellationToken: timeout.Token));

            return HealthCheckResult.Healthy("SQL Server responde.");
        }
        catch (Exception exception)
        {
            // Límite de infraestructura: el estado de salud se informa como resultado, no como excepción
            return HealthCheckResult.Unhealthy("SQL Server no responde.", exception);
        }
    }
}
