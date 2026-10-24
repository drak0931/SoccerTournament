using System.Collections.Concurrent;
using System.Reflection;
using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Abstractions.Behaviors;

/// <summary>
/// Crea un resultado fallido del tipo concreto que espera el pipeline (Result o Result&lt;T&gt;),
/// para que un behavior pueda cortar la ejecución sin lanzar excepciones.
/// </summary>
internal static class ResultFactory
{
    private static readonly MethodInfo GenericFailureMethod = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(method => method.Name == nameof(Result.Failure) && method.IsGenericMethodDefinition);

    private static readonly ConcurrentDictionary<Type, MethodInfo> FailureMethods = new();

    public static TResult Failure<TResult>(Error error)
        where TResult : Result
    {
        if (typeof(TResult) == typeof(Result))
        {
            return (TResult)Result.Failure(error);
        }

        var failureMethod = FailureMethods.GetOrAdd(
            typeof(TResult),
            static resultType => GenericFailureMethod.MakeGenericMethod(resultType.GetGenericArguments()[0]));

        return (TResult)failureMethod.Invoke(null, new object[] { error })!;
    }
}
