namespace SoccerTournament.Infrastructure.Tests.ReadSide;

/// <summary>
/// Prueba de integración opcional contra una BD real. Se omite salvo que exista la variable de entorno
/// SOCCERTOURNAMENT_TEST_DB con la cadena de conexión. Solo ejecuta consultas de lectura.
/// </summary>
public sealed class DatabaseFactAttribute : FactAttribute
{
    public const string ConnectionStringVariable = "SOCCERTOURNAMENT_TEST_DB";

    public DatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable)))
        {
            Skip = $"Defina {ConnectionStringVariable} para ejecutar las pruebas de lectura contra la BD.";
        }
    }
}
