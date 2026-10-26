using Dapper;
using SoccerTournament.Application.Abstractions.Idempotency;
using SoccerTournament.Infrastructure.ReadSide;
using SoccerTournament.Infrastructure.ReadSide.Sql;

namespace SoccerTournament.Infrastructure.Idempotency;

/// <summary>
/// Persistencia de Idempotency-Key en dbo.idempotency_records con SQL atómico (Dapper).
/// No usa el DbContext: el registro de idempotencia se confirma en su propia transacción corta,
/// antes y después del command, sin mezclarse con el Unit of Work del caso de uso.
/// </summary>
internal sealed class SqlIdempotencyStore : IIdempotencyStore
{
    private const string InProgress = "in_progress";
    private const string Completed = "completed";

    // Reclamo atómico: UPDLOCK + HOLDLOCK serializa requests concurrentes con la misma clave
    private const string ClaimSql = """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        DELETE FROM dbo.idempotency_records
        WHERE idempotency_key = @IdempotencyKey AND expires_at < SYSUTCDATETIME();

        INSERT INTO dbo.idempotency_records (idempotency_key, request_method, request_path, request_hash, status, expires_at)
        SELECT @IdempotencyKey, @Method, @Path, @RequestHash, 'in_progress', @ExpiresAt
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.idempotency_records WITH (UPDLOCK, HOLDLOCK)
            WHERE idempotency_key = @IdempotencyKey
        );

        DECLARE @Claimed BIT = CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END;

        SELECT
            @Claimed             AS Claimed,
            request_hash         AS RequestHash,
            status               AS Status,
            response_status_code AS StatusCode,
            response_body        AS Body,
            response_location    AS Location
        FROM dbo.idempotency_records
        WHERE idempotency_key = @IdempotencyKey;

        COMMIT TRANSACTION;
        """;

    private const string CompleteSql = """
        UPDATE dbo.idempotency_records
        SET status = 'completed',
            response_status_code = @StatusCode,
            response_body = @Body,
            response_location = @Location,
            completed_at = SYSUTCDATETIME()
        WHERE idempotency_key = @IdempotencyKey AND status = 'in_progress';
        """;

    private const string ReleaseSql = """
        DELETE FROM dbo.idempotency_records
        WHERE idempotency_key = @IdempotencyKey AND status = 'in_progress';
        """;

    private readonly SqlConnectionFactory _connectionFactory;

    public SqlIdempotencyStore(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IdempotencyClaim> TryClaimAsync(IdempotencyRequest request, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        var parameters = new
        {
            IdempotencyKey = new DbString { Value = request.IdempotencyKey, Length = 100 },
            Method = new DbString { Value = request.Method, IsAnsi = true, Length = 10 },
            Path = new DbString { Value = request.Path, Length = 300 },
            RequestHash = new DbString { Value = request.RequestHash, IsAnsi = true, IsFixedLength = true, Length = 64 },
            ExpiresAt = expiresAtUtc
        };

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleAsync<ClaimRow>(
            new CommandDefinition(ClaimSql, parameters, commandTimeout: PagedQuery.CommandTimeoutSeconds, cancellationToken: cancellationToken));

        if (row.Claimed)
        {
            return IdempotencyClaim.New();
        }

        var status = row.Status == Completed ? IdempotencyStatus.Completed : IdempotencyStatus.InProgress;
        var response = row.StatusCode.HasValue ? new IdempotencyResponse(row.StatusCode.Value, row.Body, row.Location) : null;

        return IdempotencyClaim.AlreadyExists(new IdempotencyRecord(row.RequestHash.Trim(), status, response));
    }

    public async Task CompleteAsync(string idempotencyKey, IdempotencyResponse response, CancellationToken cancellationToken)
    {
        var parameters = new
        {
            IdempotencyKey = new DbString { Value = idempotencyKey, Length = 100 },
            StatusCode = (short)response.StatusCode,
            response.Body,
            Location = response.Location
        };

        await ExecuteAsync(CompleteSql, parameters, cancellationToken);
    }

    public Task ReleaseAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        ExecuteAsync(ReleaseSql, new { IdempotencyKey = new DbString { Value = idempotencyKey, Length = 100 } }, cancellationToken);

    private async Task ExecuteAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, commandTimeout: PagedQuery.CommandTimeoutSeconds, cancellationToken: cancellationToken));
    }

    private sealed class ClaimRow
    {
        public bool Claimed { get; set; }

        public string RequestHash { get; set; } = null!;

        public string Status { get; set; } = InProgress;

        public short? StatusCode { get; set; }

        public string? Body { get; set; }

        public string? Location { get; set; }
    }
}
