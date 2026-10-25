using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Infrastructure.Persistence.Auditing;

namespace SoccerTournament.Infrastructure.Persistence.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(team => team.Id).HasName("pk_teams").IsClustered(false);
        builder.Property(team => team.Id).HasColumnName("uuid").ValueGeneratedNever();

        builder.Property(team => team.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(Team.FullNameMaxLength)
            .IsRequired();

        builder.Property(team => team.ShortName)
            .HasColumnName("short_name")
            .HasMaxLength(Team.ShortNameMaxLength)
            .IsRequired();

        builder.Property(team => team.FoundedYear)
            .HasColumnName("founded_year")
            .HasColumnType("datetime");

        builder.Ignore(team => team.DomainEvents);

        builder.HasAuditColumns().HasRowVersion();
    }
}
