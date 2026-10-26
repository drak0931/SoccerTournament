using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using SoccerTournament.Api.Common;
using SoccerTournament.Api.Idempotency;

namespace SoccerTournament.Api;

/// <summary>
/// Registro de dependencias de la capa Api (presentación HTTP).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers(options =>
            {
                // Las reglas de obligatoriedad las valida FluentValidation (mensajes en español), no MVC
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options =>
            {
                // Enums como texto en camelCase ("finished", "forward"); la lectura no distingue mayúsculas
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

        services.AddProblemDetails(options =>
        {
            // TraceId en formato W3C (32 hex) para enlazar la respuesta con logs y domain_events
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.Configure<IdempotencyOptions>(configuration.GetSection(IdempotencyOptions.SectionName));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Soccer Tournament API",
                Version = "v1",
                Description = "Control de torneo de fútbol: equipos, jugadores, partidos, resultados, posiciones y goleadores."
            });
            options.SupportNonNullableReferenceTypes();
            options.OperationFilter<IdempotencyHeaderOperationFilter>();
        });

        services.AddHealthChecks();

        return services;
    }
}
