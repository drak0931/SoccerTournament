using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Abstractions.Persistence.Write;

/// <summary>
/// Unidad de trabajo del lado de escritura. Es el único punto donde se confirman cambios en la BD.
/// <para>
/// <see cref="CommitAsync"/> abre una transacción, guarda los cambios de todos los repositorios,
/// registra los eventos de dominio en dbo.domain_events y confirma; ante un fallo hace rollback completo.
/// </para>
/// <para>
/// Devuelve <see cref="Result"/>: los conflictos de BD (unicidad, concurrencia) se traducen a errores
/// de tipo Conflict en lugar de propagarse como excepciones.
/// </para>
/// </summary>
public interface IUnitOfWork
{
    Task<Result> CommitAsync(CancellationToken cancellationToken = default);
}
