using Microsoft.EntityFrameworkCore;
using SoccerTournament.Infrastructure.Persistence;

namespace SoccerTournament.Infrastructure.Tests.Persistence;

/// <summary>
/// Crea un DbContext con el proveedor SQL Server real pero SIN abrir conexión:
/// permite validar el modelo y el ChangeTracker sin tocar ninguna base de datos.
/// </summary>
internal static class TestDbContextFactory
{
    private const string OfflineConnectionString = "Server=offline-model-validation;Database=ModelOnly;Integrated Security=true";

    public static ApplicationDbContext Create() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(OfflineConnectionString)
            .Options);
}
