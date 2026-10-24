namespace SoccerTournament.Application.Abstractions.Pagination;

/// <summary>
/// Respuesta paginada estándar: { data, pageNumber, pageSize, totalRecords, totalPages }.
/// </summary>
public sealed record PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> data, int pageNumber, int pageSize, int totalRecords)
    {
        Data = data;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalRecords = totalRecords;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalRecords / (double)pageSize) : 0;
    }

    public IReadOnlyList<T> Data { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalRecords { get; }

    public int TotalPages { get; }
}
