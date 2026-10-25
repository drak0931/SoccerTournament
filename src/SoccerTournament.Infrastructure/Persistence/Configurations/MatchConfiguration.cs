using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Infrastructure.Persistence.Auditing;
using SoccerTournament.Infrastructure.Persistence.Converters;

namespace SoccerTournament.Infrastructure.Persistence.Configurations;

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");

        builder.HasKey(match => match.Id).HasName("pk_matches").IsClustered(false);
        builder.Property(match => match.Id).HasColumnName("uuid").ValueGeneratedNever();

        builder.Property(match => match.HomeTeamId).HasColumnName("home_team_id");
        builder.Property(match => match.AwayTeamId).HasColumnName("away_team_id");
        builder.Property(match => match.ScheduledAt).HasColumnName("scheduled_at").HasPrecision(3);

        builder.Property(match => match.Status)
            .HasColumnName("status")
            .HasConversion<MatchStatusConverter>()
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.Property(match => match.HomeGoals).HasColumnName("home_goals");
        builder.Property(match => match.AwayGoals).HasColumnName("away_goals");
        builder.Property(match => match.ResultRegisteredAt).HasColumnName("result_registered_at").HasPrecision(3);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(match => match.HomeTeamId)
            .HasConstraintName("fk_matches_home_team")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(match => match.AwayTeamId)
            .HasConstraintName("fk_matches_away_team")
            .OnDelete(DeleteBehavior.Restrict);

        // Los goles pertenecen al agregado: se cargan y guardan junto con el partido
        builder.HasMany(match => match.Goals)
            .WithOne()
            .HasForeignKey(goal => goal.MatchId)
            .HasConstraintName("fk_goals_matches")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(match => match.Goals).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(match => match.DomainEvents);

        builder.HasAuditColumns().HasRowVersion();
    }
}
