using System.Collections.Concurrent;
using SoccerTournament.Application.Abstractions.Idempotency;

namespace SoccerTournament.Api.Tests.Infrastructure;

/// <summary>
/// Implementación en memoria con la misma semántica que SqlIdempotencyStore (reclamo atómico por clave).
/// </summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _records = new();

    public int CompletedCount => _records.Values.Count(record => record.Status == IdempotencyStatus.Completed);

    public bool Contains(string idempotencyKey) => _records.ContainsKey(idempotencyKey);

    public void SeedInProgress(string idempotencyKey, string requestHash) =>
        _records[idempotencyKey] = new IdempotencyRecord(requestHash, IdempotencyStatus.InProgress, null);

    public string? GetHash(string idempotencyKey) =>
        _records.TryGetValue(idempotencyKey, out var record) ? record.RequestHash : null;

    public Task<IdempotencyClaim> TryClaimAsync(IdempotencyRequest request, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        var inProgress = new IdempotencyRecord(request.RequestHash, IdempotencyStatus.InProgress, null);

        return Task.FromResult(_records.TryAdd(request.IdempotencyKey, inProgress)
            ? IdempotencyClaim.New()
            : IdempotencyClaim.AlreadyExists(_records[request.IdempotencyKey]));
    }

    public Task CompleteAsync(string idempotencyKey, IdempotencyResponse response, CancellationToken cancellationToken)
    {
        _records.AddOrUpdate(
            idempotencyKey,
            _ => throw new InvalidOperationException("Clave no reclamada."),
            (_, existing) => existing with { Status = IdempotencyStatus.Completed, Response = response });

        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        _records.TryRemove(idempotencyKey, out _);
        return Task.CompletedTask;
    }
}
