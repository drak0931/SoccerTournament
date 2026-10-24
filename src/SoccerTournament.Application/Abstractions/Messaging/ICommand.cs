namespace SoccerTournament.Application.Abstractions.Messaging;

/// <summary>
/// Marcador común del lado de escritura (write side). Permite identificar commands en pipelines y pruebas.
/// </summary>
public interface ICommandBase
{
}

/// <summary>
/// Command sin valor de retorno: su handler devuelve <c>Result</c>.
/// </summary>
public interface ICommand : ICommandBase
{
}

/// <summary>
/// Command que devuelve un valor: su handler devuelve <c>Result&lt;TResponse&gt;</c>.
/// </summary>
public interface ICommand<TResponse> : ICommandBase
{
}
