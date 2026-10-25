using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Infrastructure.Persistence.Auditing;

namespace SoccerTournament.Infrastructure.Persistence.Configurations;

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("players");

        builder.HasKey(player => player.Id).HasName("pk_players").IsClustered(false);
        builder.Property(player => player.Id).HasColumnName("uuid").ValueGeneratedNever();

        // Clave alterna (uuid, team_id): principal de la FK compuesta fk_goals_player_team
        builder.HasAlternateKey(player => new { player.Id, player.TeamId }).HasName("uq_players_uuid_team");

        builder.Property(player => player.TeamId).HasColumnName("team_id");

        builder.Property(player => player.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(Player.NameMaxLength);

        builder.Property(player => player.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(Player.NameMaxLength)
            .IsRequired();

        builder.Property(player => player.JerseyNumber).HasColumnName("jersey_number");

        builder.Property(player => player.Position)
            .HasColumnName("position")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(player => player.TeamId)
            .HasConstraintName("fk_players_teams")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(player => player.DomainEvents);

        builder.HasAuditColumns().HasRowVersion();
    }
}
