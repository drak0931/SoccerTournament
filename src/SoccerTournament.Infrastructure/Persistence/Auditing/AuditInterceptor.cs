using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SoccerTournament.Application.Abstractions.Clock;

namespace SoccerTournament.Infrastructure.Persistence.Auditing;

/// <summary>
/// Completa las columnas de auditoría antes de guardar. Como la prueba no incluye autenticación,
/// todas las operaciones se atribuyen al usuario técnico del sistema (<see cref="SystemUser"/>).
/// </summary>
internal sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditInterceptor(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    internal void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = _dateTimeProvider.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var metadata = entry.Metadata;

            if (entry.State == EntityState.Added && metadata.FindProperty(AuditColumns.CreatedBy) is not null)
            {
                entry.Property(AuditColumns.CreatedBy).CurrentValue = SystemUser.Id;
            }

            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            if (metadata.FindProperty(AuditColumns.ModifiedAt) is not null)
            {
                entry.Property(AuditColumns.ModifiedAt).CurrentValue = utcNow;
            }

            if (metadata.FindProperty(AuditColumns.ModifiedBy) is not null)
            {
                entry.Property(AuditColumns.ModifiedBy).CurrentValue = SystemUser.Id;
            }
        }
    }
}
