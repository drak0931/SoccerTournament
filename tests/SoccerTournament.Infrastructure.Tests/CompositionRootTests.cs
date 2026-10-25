using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoccerTournament.Application;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Features.Players.Queries;
using SoccerTournament.Application.Features.Scorers.Queries;
using SoccerTournament.Application.Features.Standings.Queries;
using SoccerTournament.Application.Features.Teams.Queries;
using SoccerTournament.Application.Features.Matches.Queries;
using SoccerTournament.Infrastructure.ReadSide;

namespace SoccerTournament.Infrastructure.Tests;

/// <summary>
/// Construye el contenedor de dependencias de Application + Infrastructure con la misma validación
/// que ASP.NET Core aplica en Development (ValidateOnBuild). Detecta registros faltantes sin arrancar la API.
/// </summary>
public class CompositionRootTests
{
    [Fact]
    public void ServiceProvider_ShouldResolveEveryRegisteredService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SoccerTournament"] = "Server=offline-composition-test;Database=None;Integrated Security=true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // Lanza AggregateException si algún servicio no puede construirse (no abre conexiones)
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDispatcher>());
    }

    [Fact]
    public void AddInfrastructure_WithoutConnectionString_ShouldFailFastWithClearMessage()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(configuration));

        Assert.Contains("ConnectionStrings:SoccerTournament", exception.Message);
    }

    [Fact]
    public void AddInfrastructure_WithMalformedConnectionString_ShouldFailFastWithoutExposingIt()
    {
        // Simula una cadena copiada con un carácter invisible pegado a una palabra clave
        const string malformed = "Server=(localdb)\\MSSQLLocalDB;Database=SoccerTournament;​TrustServerCertificate=True";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:SoccerTournament"] = malformed })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(configuration));

        Assert.Contains("no tiene un formato válido", exception.Message);
        Assert.DoesNotContain("MSSQLLocalDB", exception.Message);
    }

    [Theory]
    [MemberData(nameof(SortWhitelists))]
    public void SortableFields_InApplication_ShouldMatchSqlWhitelist(IReadOnlyCollection<string> applicationFields, IEnumerable<string> sqlFields)
    {
        // Si se agrega un campo ordenable en Application sin mapearlo a SQL (o al revés), esta prueba falla
        Assert.Equal(
            applicationFields.OrderBy(field => field, StringComparer.OrdinalIgnoreCase),
            sqlFields.OrderBy(field => field, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }

    public static TheoryData<IReadOnlyCollection<string>, IEnumerable<string>> SortWhitelists() => new()
    {
        { GetTeamsQuery.SortableFields, TeamReadService.Sort.Fields },
        { GetPlayersQuery.SortableFields, PlayerReadService.Sort.Fields },
        { GetMatchesQuery.SortableFields, MatchReadService.Sort.Fields },
        { GetStandingsQuery.SortableFields, StandingReadService.Sort.Fields },
        { GetTopScorersQuery.SortableFields, ScorerReadService.Sort.Fields }
    };
}
