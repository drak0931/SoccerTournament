using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Players;

namespace SoccerTournament.Api.Contracts;

// Contratos HTTP de entrada. Se separan de los commands para que el formato público de la API
// pueda evolucionar sin acoplarse a los casos de uso (los ids de ruta no viajan en el body).

/// <param name="FoundedYear">Fecha de fundación (ISO 8601, ej. "1925-03-15").</param>
public sealed record CreateTeamRequest(string FullName, string ShortName, DateTime? FoundedYear);

/// <summary>
/// PUT: representación completa. Un campo omitido se interpreta como vacío/null (reemplazo total).
/// </summary>
public sealed record UpdateTeamRequest(string FullName, string ShortName, DateTime? FoundedYear);

public sealed record RegisterPlayerRequest(string? FirstName, string LastName, int JerseyNumber, PlayerPosition Position);

/// <param name="ScheduledAt">Fecha y hora con zona horaria (ej. "2026-11-01T19:00:00-06:00"); se guarda en UTC.</param>
public sealed record ScheduleMatchRequest(Guid HomeTeamId, Guid AwayTeamId, DateTimeOffset ScheduledAt);

public sealed record GoalRequest(Guid PlayerId, Guid TeamId, int Minute);

public sealed record RegisterMatchResultRequest(int HomeGoals, int AwayGoals, IReadOnlyList<GoalRequest>? Goals);

/// <summary>
/// PATCH de partido: actualmente solo admite la transición a "cancelled".
/// </summary>
public sealed record UpdateMatchStatusRequest(MatchStatus Status);
