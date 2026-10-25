using System.Data.Common;
using Dapper;
using SoccerTournament.Application.Abstractions.Pagination;

namespace SoccerTournament.Infrastructure.ReadSide.Sql;

/// <summary>
/// Ejecuta una página y el total de registros en UN solo viaje a la BD (dos SELECT en un batch).
/// La paginación ocurre en SQL Server con OFFSET/FETCH: nunca se trae la tabla completa a memoria.
/// </summary>
internal static class PagedQuery
{
    public const int CommandTimeoutSeconds = 30;

    /// <param name="pageSql">SELECT con su ORDER BY; aquí se agrega OFFSET/FETCH.</param>
    /// <param name="countSql">SELECT COUNT(*) con el mismo FROM/WHERE.</param>
    public static async Task<PagedResult<TResponse>> ExecuteAsync<TRow, TResponse>(
        DbConnection connection,
        string pageSql,
        string countSql,
        SqlFilter filter,
        PagedRequest request,
        Func<TRow, TResponse> map,
        CancellationToken cancellationToken)
    {
        filter.AddParameter("Offset", request.Offset).AddParameter("PageSize", request.PageSize);

        var sql = $"""
            {pageSql}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            {countSql};
            """;

        var command = new CommandDefinition(sql, filter.Parameters, commandTimeout: CommandTimeoutSeconds, cancellationToken: cancellationToken);

        await using var results = await connection.QueryMultipleAsync(command);
        var rows = (await results.ReadAsync<TRow>()).Select(map).ToList();
        var totalRecords = await results.ReadSingleAsync<int>();

        return new PagedResult<TResponse>(rows, request.PageNumber, request.PageSize, totalRecords);
    }

    /// <summary>
    /// SQL Server no guarda el DateTimeKind: las fechas se almacenan en UTC y así se exponen.
    /// </summary>
    public static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static DateTime? AsUtc(DateTime? value) => value.HasValue ? AsUtc(value.Value) : null;
}
