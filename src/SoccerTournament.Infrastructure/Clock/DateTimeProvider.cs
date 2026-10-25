using SoccerTournament.Application.Abstractions.Clock;

namespace SoccerTournament.Infrastructure.Clock;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
