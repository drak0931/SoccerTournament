using FluentValidation;

namespace SoccerTournament.Application.Abstractions.Pagination;

/// <summary>
/// Reglas comunes de paginación. Cada consulta indica su lista blanca de campos ordenables,
/// lo que evita inyección SQL en el ORDER BY dinámico de Dapper.
/// </summary>
public abstract class PagedRequestValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : PagedRequest
{
    protected PagedRequestValidator(IReadOnlyCollection<string> sortableFields)
    {
        RuleFor(request => request.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, PagedRequest.MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {PagedRequest.MaxPageSize}.");

        RuleFor(request => request.SortDirection)
            .IsInEnum()
            .When(request => request.SortDirection.HasValue)
            .WithMessage("La dirección de ordenamiento debe ser 'asc' o 'desc'.");

        RuleFor(request => request.SortBy)
            .Must(sortBy => sortBy is null || sortableFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Campo de ordenamiento no permitido. Valores válidos: {string.Join(", ", sortableFields)}.");
    }
}
