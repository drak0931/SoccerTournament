namespace SoccerTournament.Application.Abstractions.Pagination;

/// <summary>
/// Parámetros comunes de paginación y ordenamiento para todas las consultas GET.
/// La paginación se ejecuta en la BD (OFFSET/FETCH); nunca se trae todo a memoria.
/// </summary>
public abstract record PagedRequest
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public int PageNumber { get; init; } = DefaultPageNumber;

    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>
    /// Campo de ordenamiento. Solo se aceptan los valores de la lista blanca de cada consulta.
    /// </summary>
    public string? SortBy { get; init; }

    public SortDirection SortDirection { get; init; } = SortDirection.Asc;

    /// <summary>
    /// Filas a omitir. Es long para evitar desbordamiento con números de página muy grandes (OFFSET acepta BIGINT).
    /// </summary>
    public long Offset => (PageNumber - 1L) * PageSize;
}

public enum SortDirection
{
    Asc = 0,
    Desc = 1
}
