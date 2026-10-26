using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Serilog.Core;
using SoccerTournament.Application.Abstractions.Idempotency;
using SoccerTournament.Application.Abstractions.Messaging;

namespace SoccerTournament.Api.Tests.Infrastructure;

/// <summary>
/// Levanta la API completa en memoria (routing, filtros, JSON, ProblemDetails) sin base de datos:
/// el dispatcher y el almacén de idempotencia se reemplazan por dobles de prueba.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public IDispatcher Dispatcher { get; } = Substitute.For<IDispatcher>();

    public InMemoryIdempotencyStore IdempotencyStore { get; } = new();

    public CollectingLogSink LogSink { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SoccerTournament", "Server=offline-api-tests;Database=None;Integrated Security=true");
        builder.UseSetting("Database:RunMigrationsOnStartup", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDispatcher>();
            services.AddSingleton(Dispatcher);

            services.RemoveAll<IIdempotencyStore>();
            services.AddSingleton<IIdempotencyStore>(IdempotencyStore);

            // Serilog toma los sinks registrados en DI (ReadFrom.Services)
            services.AddSingleton<ILogEventSink>(LogSink);
        });
    }
}
