using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SoccerTournament.Application.Abstractions.Behaviors;
using SoccerTournament.Application.Abstractions.Events;
using SoccerTournament.Application.Abstractions.Messaging;

namespace SoccerTournament.Application;

/// <summary>
/// Registro de dependencias de la capa Application.
/// </summary>
public static class DependencyInjection
{
    private static readonly Type[] HandlerInterfaces =
    {
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IDomainEventHandler<>)
    };

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        RegisterHandlers(services);

        // Orden del pipeline: el primero registrado es el más externo → Logging → Validation → Handler
        services.AddScoped(typeof(ICommandPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(ICommandPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddValidatorsFromAssembly(AssemblyReference.Assembly, includeInternalTypes: true);

        // Mensajes de validación en español, independientemente de la cultura del servidor
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");

        return services;
    }

    // Escanea el ensamblado y registra cada handler contra las interfaces que implementa
    private static void RegisterHandlers(IServiceCollection services)
    {
        var registrations = AssemblyReference.Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType && HandlerInterfaces.Contains(contract.GetGenericTypeDefinition()))
                .Select(contract => (Contract: contract, Implementation: type)));

        foreach (var (contract, implementation) in registrations)
        {
            services.AddScoped(contract, implementation);
        }
    }
}
