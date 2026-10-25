using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SoccerTournament.Domain.Matches;

namespace SoccerTournament.Infrastructure.Persistence.Converters;

/// <summary>
/// Marca como UTC las fechas leídas de la BD (SQL Server no almacena el DateTimeKind).
/// </summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}

internal sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            value => value.HasValue && value.Value.Kind == DateTimeKind.Local ? value.Value.ToUniversalTime() : value,
            value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value)
    {
    }
}

/// <summary>
/// MatchStatus ↔ 'scheduled' | 'finished' | 'cancelled' (valores de ck_matches_status).
/// </summary>
internal sealed class MatchStatusConverter : ValueConverter<MatchStatus, string>
{
    public MatchStatusConverter()
        : base(
            value => value.ToString().ToLowerInvariant(),
            value => Enum.Parse<MatchStatus>(value, true))
    {
    }
}
