using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Infrastructure.Persistence.Auditing;

namespace SoccerTournament.Infrastructure.Persistence.Configurations;

internal sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("goals");

        builder.HasKey(goal => goal.Id).HasName("pk_goals").IsClustered(false);
        builder.Property(goal => goal.Id).HasColumnName("uuid").ValueGeneratedNever();

        builder.Property(goal => goal.MatchId).HasColumnName("match_id");
        builder.Property(goal => goal.PlayerId).HasColumnName("player_id");
        builder.Property(goal => goal.TeamId).HasColumnName("team_id");
        builder.Property(goal => goal.Minute).HasColumnName("minute");

        // El goleador debe pertenecer al equipo al que se acredita el gol
        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(goal => new { goal.PlayerId, goal.TeamId })
            .HasPrincipalKey(player => new { player.Id, player.TeamId })
            .HasConstraintName("fk_goals_player_team")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(goal => goal.TeamId)
            .HasConstraintName("fk_goals_teams")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAuditColumns();
    }
}
