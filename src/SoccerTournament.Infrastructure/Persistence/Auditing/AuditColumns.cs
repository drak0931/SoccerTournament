using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SoccerTournament.Infrastructure.Persistence.Auditing;

/// <summary>
/// Columnas de auditoría y concurrencia mapeadas como propiedades sombra:
/// el dominio no las conoce, Infrastructure las administra.
/// </summary>
internal static class AuditColumns
{
    public const string CreatedAt = nameof(CreatedAt);
    public const string CreatedBy = nameof(CreatedBy);
    public const string ModifiedAt = nameof(ModifiedAt);
    public const string ModifiedBy = nameof(ModifiedBy);
    public const string RowVersion = nameof(RowVersion);

    /// <summary>
    /// created_at (default SYSUTCDATETIME en BD), created_by, modified_at, modified_by.
    /// </summary>
    public static EntityTypeBuilder<TEntity> HasAuditColumns<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<DateTime>(CreatedAt)
            .HasColumnName("created_at")
            .HasPrecision(3)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.Property<Guid>(CreatedBy)
            .HasColumnName("created_by")
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasModifiedAt();

        builder.Property<Guid?>(ModifiedBy).HasColumnName("modified_by");

        return builder;
    }

    public static EntityTypeBuilder<TEntity> HasModifiedAt<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<DateTime?>(ModifiedAt).HasColumnName("modified_at").HasPrecision(3);
        return builder;
    }

    /// <summary>
    /// row_version (ROWVERSION) como token de concurrencia optimista.
    /// </summary>
    public static EntityTypeBuilder<TEntity> HasRowVersion<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<byte[]>(RowVersion).HasColumnName("row_version").IsRowVersion();
        return builder;
    }
}
