using SoccerTournament.Application.Abstractions.Pagination;

namespace SoccerTournament.Infrastructure.ReadSide.Sql;

/// <summary>
/// Lista blanca de ordenamiento: traduce el nombre público del campo (sortBy) a una expresión SQL fija.
/// Siempre agrega un desempate por clave para que la paginación sea determinista.
/// </summary>
internal sealed class SortMap
{
    private readonly IReadOnlyDictionary<string, string> _columns;
    private readonly string _defaultOrderBy;
    private readonly string _tieBreaker;

    public SortMap(IReadOnlyDictionary<string, string> columns, string defaultOrderBy, string tieBreaker)
    {
        _columns = new Dictionary<string, string>(columns, StringComparer.OrdinalIgnoreCase);
        _defaultOrderBy = defaultOrderBy;
        _tieBreaker = tieBreaker;
    }

    public IEnumerable<string> Fields => _columns.Keys;

    public string ToOrderByClause(string? sortBy, SortDirection direction)
    {
        // Un campo fuera de la lista blanca ya fue rechazado por el validador (400); aquí se usa el orden por defecto
        if (sortBy is null || !_columns.TryGetValue(sortBy, out var column))
        {
            return $"ORDER BY {_defaultOrderBy}, {_tieBreaker}";
        }

        var sqlDirection = direction == SortDirection.Desc ? "DESC" : "ASC";
        return $"ORDER BY {column} {sqlDirection}, {_tieBreaker}";
    }
}
