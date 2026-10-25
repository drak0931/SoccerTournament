using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoccerTournament.Infrastructure.Persistence.DomainEvents;

namespace SoccerTournament.Infrastructure.Persistence.Configurations;

internal sealed class DomainEventRecordConfiguration : IEntityTypeConfiguration<DomainEventRecord>
{
    public void Configure(EntityTypeBuilder<DomainEventRecord> builder)
    {
        builder.ToTable("domain_events");

        builder.HasKey(record => record.Id).HasName("pk_domain_events").IsClustered(false);
        builder.Property(record => record.Id).HasColumnName("uuid").ValueGeneratedNever();

        builder.Property(record => record.EventType).HasColumnName("event_type").HasMaxLength(200).IsRequired();
        builder.Property(record => record.AggregateType).HasColumnName("aggregate_type").HasMaxLength(100).IsRequired();
        builder.Property(record => record.AggregateId).HasColumnName("aggregate_id");
        builder.Property(record => record.Payload).HasColumnName("payload").IsRequired();
        builder.Property(record => record.OccurredAt).HasColumnName("occurred_at").HasPrecision(3);
        builder.Property(record => record.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64);
        builder.Property(record => record.TraceId).HasColumnName("trace_id").HasMaxLength(32).IsUnicode(false);
    }
}
