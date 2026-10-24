using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Abstractions.Messaging;

/// <summary>
/// Implementación propia del mediador. Resuelve el handler del request y lo envuelve con los
/// comportamientos registrados (el primero registrado es el más externo).
/// Los wrappers genéricos se crean una sola vez por tipo y se cachean para evitar reflexión en cada llamada.
/// </summary>
internal sealed class Dispatcher : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, VoidCommandWrapper> CommandWrappers = new();
    private static readonly ConcurrentDictionary<Type, object> CommandWithResponseWrappers = new();
    private static readonly ConcurrentDictionary<Type, object> QueryWrappers = new();

    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<Result> SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var wrapper = CommandWrappers.GetOrAdd(
            command.GetType(),
            static type => (VoidCommandWrapper)Activator.CreateInstance(typeof(VoidCommandWrapper<>).MakeGenericType(type))!);

        return wrapper.HandleAsync(command, _serviceProvider, cancellationToken);
    }

    public Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        var wrapper = (ResponseCommandWrapper<TResponse>)CommandWithResponseWrappers.GetOrAdd(
            command.GetType(),
            static type => Activator.CreateInstance(typeof(ResponseCommandWrapper<,>).MakeGenericType(type, typeof(TResponse)))!);

        return wrapper.HandleAsync(command, _serviceProvider, cancellationToken);
    }

    public Task<Result<TResponse>> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        var wrapper = (QueryWrapper<TResponse>)QueryWrappers.GetOrAdd(
            query.GetType(),
            static type => Activator.CreateInstance(typeof(QueryWrapper<,>).MakeGenericType(type, typeof(TResponse)))!);

        return wrapper.HandleAsync(query, _serviceProvider, cancellationToken);
    }

    // Construye la cadena: behavior[0](behavior[1](...(handler)))
    private static RequestHandlerDelegate<TResult> BuildPipeline<TRequest, TResult>(
        TRequest request,
        RequestHandlerDelegate<TResult> handler,
        IEnumerable<Func<TRequest, RequestHandlerDelegate<TResult>, Task<TResult>>> behaviors)
        where TResult : Result
    {
        var pipeline = handler;

        foreach (var behavior in behaviors.Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior(request, next);
        }

        return pipeline;
    }

    private abstract class VoidCommandWrapper
    {
        public abstract Task<Result> HandleAsync(ICommand command, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class VoidCommandWrapper<TCommand> : VoidCommandWrapper
        where TCommand : ICommand
    {
        public override Task<Result> HandleAsync(ICommand command, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var typedCommand = (TCommand)command;
            var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
            var behaviors = serviceProvider
                .GetServices<ICommandPipelineBehavior<TCommand, Result>>()
                .Select(behavior => (Func<TCommand, RequestHandlerDelegate<Result>, Task<Result>>)
                    ((request, next) => behavior.HandleAsync(request, next, cancellationToken)));

            return BuildPipeline(typedCommand, () => handler.HandleAsync(typedCommand, cancellationToken), behaviors)();
        }
    }

    private abstract class ResponseCommandWrapper<TResponse>
    {
        public abstract Task<Result<TResponse>> HandleAsync(ICommand<TResponse> command, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class ResponseCommandWrapper<TCommand, TResponse> : ResponseCommandWrapper<TResponse>
        where TCommand : ICommand<TResponse>
    {
        public override Task<Result<TResponse>> HandleAsync(ICommand<TResponse> command, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var typedCommand = (TCommand)command;
            var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
            var behaviors = serviceProvider
                .GetServices<ICommandPipelineBehavior<TCommand, Result<TResponse>>>()
                .Select(behavior => (Func<TCommand, RequestHandlerDelegate<Result<TResponse>>, Task<Result<TResponse>>>)
                    ((request, next) => behavior.HandleAsync(request, next, cancellationToken)));

            return BuildPipeline(typedCommand, () => handler.HandleAsync(typedCommand, cancellationToken), behaviors)();
        }
    }

    private abstract class QueryWrapper<TResponse>
    {
        public abstract Task<Result<TResponse>> HandleAsync(IQuery<TResponse> query, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class QueryWrapper<TQuery, TResponse> : QueryWrapper<TResponse>
        where TQuery : IQuery<TResponse>
    {
        public override Task<Result<TResponse>> HandleAsync(IQuery<TResponse> query, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var typedQuery = (TQuery)query;
            var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
            var behaviors = serviceProvider
                .GetServices<IQueryPipelineBehavior<TQuery, Result<TResponse>>>()
                .Select(behavior => (Func<TQuery, RequestHandlerDelegate<Result<TResponse>>, Task<Result<TResponse>>>)
                    ((request, next) => behavior.HandleAsync(request, next, cancellationToken)));

            return BuildPipeline(typedQuery, () => handler.HandleAsync(typedQuery, cancellationToken), behaviors)();
        }
    }
}
