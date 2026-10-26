using Microsoft.AspNetCore.Mvc;

namespace SoccerTournament.Api.Idempotency;

/// <summary>
/// Marca una acción POST como idempotente: exige el header Idempotency-Key y aplica <see cref="IdempotencyFilter"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : TypeFilterAttribute
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayedHeaderName = "Idempotency-Replayed";

    public IdempotentAttribute()
        : base(typeof(IdempotencyFilter))
    {
    }
}
