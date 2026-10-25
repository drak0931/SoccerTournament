namespace SoccerTournament.Infrastructure.Persistence;

/// <summary>
/// Cadena de conexión validada al iniciar. La comparten EF Core (escritura), DbUp y Dapper (lectura).
/// </summary>
internal sealed class SqlConnectionStringProvider
{
    public SqlConnectionStringProvider(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }
}
