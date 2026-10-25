using Dapper;

namespace SoccerTournament.Infrastructure.ReadSide.Sql;

/// <summary>
/// Construye un WHERE dinámico con SOLO los filtros enviados. Cada condición es SQL fijo escrito en código
/// y los valores viajan como parámetros: la entrada del usuario nunca se concatena en el SQL.
/// </summary>
internal sealed class SqlFilter
{
    private readonly List<string> _conditions = new();

    public DynamicParameters Parameters { get; } = new();

    public SqlFilter Add(bool condition, string sqlCondition, string parameterName, object? value)
    {
        if (condition)
        {
            _conditions.Add(sqlCondition);
            Parameters.Add(parameterName, value);
        }

        return this;
    }

    public SqlFilter AddParameter(string parameterName, object? value)
    {
        Parameters.Add(parameterName, value);
        return this;
    }

    public string ToWhereClause() =>
        _conditions.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", _conditions);

    /// <summary>
    /// Patrón LIKE "contiene" escapando los comodines de SQL Server ([, %, _) del texto del usuario.
    /// </summary>
    public static string ContainsPattern(string value)
    {
        var escaped = value.Trim()
            .Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);

        return $"%{escaped}%";
    }
}
