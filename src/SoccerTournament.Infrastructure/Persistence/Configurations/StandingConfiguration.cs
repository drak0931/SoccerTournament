using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoccerTournament.Domain.Standings;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Infrastructure.Persistence.Auditing;

namespace SoccerTournament.Infrastructure.Persistence.Configurations;

internal sealed class StandingConfiguration : IEntityTypeConfiguration<Standing>
{
    public void Configure(EntityTypeBuilder<Standing> builder)
    {
        builder.ToTable("standings");

        // El Id del agregado es el Id del equipo (PK clúster team_id)
        builder.HasKey(standing => standing.Id).HasName("pk_standings").IsClustered();
        builder.Property(standing => standing.Id).HasColumnName("team_id").ValueGeneratedNever();
        builder.Ignore(standing => standing.TeamId);

        // Columnas SMALLINT expuestas como int en el dominio
        builder.Property(standing => standing.Played).HasColumnName("played").HasConversion<short>();
        builder.Property(standing => standing.Won).HasColumnName("won").HasConversion<short>();
        builder.Property(standing => standing.Drawn).HasColumnName("drawn").HasConversion<short>();
        builder.Property(standing => standing.Lost).HasColumnName("lost").HasConversion<short>();
        builder.Property(standing => standing.GoalsFor).HasColumnName("goals_for").HasConversion<short>();
        builder.Property(standing => standing.GoalsAgainst).HasColumnName("goals_against").HasConversion<short>();
        builder.Property(standing => standing.GoalDifference).HasColumnName("goal_difference").HasConversion<short>();
        builder.Property(standing => standing.Points).HasColumnName("points").HasConversion<short>();

        // Relación 1:1 con el equipo; la BD elimina la fila en cascada al borrar el equipo
        builder.HasOne<Team>()
            .WithOne()
            .HasForeignKey<Standing>(standing => standing.Id)
            .HasConstraintName("fk_standings_teams")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(standing => standing.DomainEvents);

        builder.HasModifiedAt().HasRowVersion();
    }
}
