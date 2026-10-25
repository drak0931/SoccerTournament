using Microsoft.EntityFrameworkCore;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Standings;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Infrastructure.Persistence.Converters;
using SoccerTournament.Infrastructure.Persistence.DomainEvents;

namespace SoccerTournament.Infrastructure.Persistence;

/// <summary>
/// DbContext del lado de escritura. Mapea el esquema existente (creado por scripts DbUp, sin migraciones EF).
/// Nunca se invoca SaveChanges fuera de <see cref="UnitOfWork"/>.
/// </summary>
internal sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<Standing> Standings => Set<Standing>();

    public DbSet<DomainEventRecord> DomainEventRecords => Set<DomainEventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Todas las fechas se guardan en UTC; al leerlas se marcan como DateTimeKind.Utc
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }
}
