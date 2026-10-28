using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using SoccerTournament.Application.Abstractions.Diagnostics;
using SoccerTournament.Infrastructure.Diagnostics;

namespace SoccerTournament.Api.Observability;

/// <summary>
/// Logging estructurado (Serilog, JSON) y trazas distribuidas (OpenTelemetry → OTLP → Jaeger).
/// </summary>
public static class ObservabilityExtensions
{
    public const string ServiceName = "SoccerTournament.Api";

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        // La configuración (niveles, sink, formato JSON) se lee de la sección "Serilog" de appsettings.
        // preserveStaticLogger: ILogger<T> escribe en el logger de esta aplicación (DI), no en el estático Log.Logger
        services.AddSerilog(
            (serviceProvider, logger) => logger
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.FromLogContext()
                .Enrich.With<ActivityEnricher>()
                .Enrich.WithProperty("Application", ServiceName),
            preserveStaticLogger: true);

        var otlpEndpoint = configuration["Observability:OtlpEndpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ApplicationDiagnostics.ActivitySourceName)
                    .AddSource(InfrastructureDiagnostics.ActivitySourceName)
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation()
                    // Spans de SQL para EF Core (escritura) y Dapper (lectura): texto parametrizado, nunca los valores
                    .AddSqlClientInstrumentation();

                // Sin endpoint configurado las trazas se generan (TraceId en logs y BD) pero no se exportan
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }

    /// <summary>
    /// Activa el exportador de trazas antes de que el host arranque. Sin esto, el trabajo previo a
    /// app.Run() (scripts de BD y seed) genera TraceId pero sus spans no llegan a Jaeger.
    /// </summary>
    public static void StartTracing(this IServiceProvider services) => services.GetService<TracerProvider>();

    public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Logger de la aplicación (DI) en lugar del estático Log.Logger, compartido por todo el proceso
        var logger = app.ApplicationServices.GetService<Serilog.ILogger>();
        app.UseSerilogRequestLogging(options =>
        {
            RequestLogging.Configure(options);
            options.Logger = logger;
        });

        return app;
    }
}
