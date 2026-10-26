using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SoccerTournament.Api.Idempotency;

/// <summary>
/// Documenta en Swagger el header Idempotency-Key (obligatorio) en las acciones marcadas con [Idempotent].
/// </summary>
public sealed class IdempotencyHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttribute<IdempotentAttribute>() is null)
        {
            return;
        }

        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = IdempotentAttribute.HeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Clave única por operación (ej. un UUID). Repetir la misma clave con el mismo cuerpo devuelve la respuesta original.",
            Schema = new OpenApiSchema { Type = "string", MaxLength = 100, Example = new OpenApiString(Guid.NewGuid().ToString()) }
        });

        operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Idempotency-Key reutilizada con otro cuerpo, o solicitud aún en proceso." });
    }
}
