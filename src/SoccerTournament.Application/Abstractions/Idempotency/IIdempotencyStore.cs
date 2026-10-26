namespace SoccerTournament.Application.Abstractions.Idempotency;

/// <summary>
/// Almacenamiento de claves de idempotencia (header Idempotency-Key) para requests POST.
/// Es deduplicación de requests HTTP, no un caso de uso de negocio: usa transacciones cortas propias,
/// independientes del Unit of Work del command.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Reclama la clave de forma atómica. Si ya existe (y no expiró) devuelve el registro existente sin reclamarla.
    /// </summary>
    Task<IdempotencyClaim> TryClaimAsync(IdempotencyRequest request, DateTime expiresAtUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Guarda la respuesta original para devolverla idéntica ante repeticiones.
    /// </summary>
    Task CompleteAsync(string idempotencyKey, IdempotencyResponse response, CancellationToken cancellationToken);

    /// <summary>
    /// Libera una clave reclamada cuyo procesamiento falló de forma inesperada (5xx), para permitir reintentos.
    /// </summary>
    Task ReleaseAsync(string idempotencyKey, CancellationToken cancellationToken);
}

public sealed record IdempotencyRequest(string IdempotencyKey, string Method, string Path, string RequestHash);

public sealed record IdempotencyResponse(int StatusCode, string? Body, string? Location);

public enum IdempotencyStatus
{
    InProgress = 1,
    Completed = 2
}

public sealed record IdempotencyRecord(string RequestHash, IdempotencyStatus Status, IdempotencyResponse? Response);

/// <summary>
/// Resultado de reclamar una clave: <see cref="Claimed"/> = true si este request debe procesarse;
/// en caso contrario <see cref="Existing"/> contiene el registro previo.
/// </summary>
public sealed record IdempotencyClaim(bool Claimed, IdempotencyRecord? Existing)
{
    public static IdempotencyClaim New() => new(true, null);

    public static IdempotencyClaim AlreadyExists(IdempotencyRecord existing) => new(false, existing);
}
