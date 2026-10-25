using System.Data.Common;
using Microsoft.Data.SqlClient;
using SoccerTournament.Infrastructure.Persistence;

namespace SoccerTournament.Infrastructure.ReadSide;

/// <summary>
/// Crea conexiones ADO.NET para el lado de lectura (Dapper). Independiente del DbContext de EF Core.
/// </summary>
internal sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(SqlConnectionStringProvider connectionStringProvider)
    {
        _connectionString = connectionStringProvider.ConnectionString;
    }

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
