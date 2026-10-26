using System.Reflection;
using NetArchTest.Rules;

namespace SoccerTournament.ArchitectureTests;

/// <summary>
/// Valida las reglas de Clean Architecture:
/// - Domain no depende de ninguna otra capa.
/// - Application depende solo de Domain.
/// - Infrastructure implementa contratos y no conoce la Api.
/// </summary>
public class LayerDependencyTests
{
    private const string DomainNamespace = "SoccerTournament.Domain";
    private const string ApplicationNamespace = "SoccerTournament.Application";
    private const string InfrastructureNamespace = "SoccerTournament.Infrastructure";
    private const string ApiNamespace = "SoccerTournament.Api";

    private static readonly Assembly DomainAssembly = Domain.AssemblyReference.Assembly;
    private static readonly Assembly ApplicationAssembly = Application.AssemblyReference.Assembly;
    private static readonly Assembly InfrastructureAssembly = Infrastructure.AssemblyReference.Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Domain_ShouldNotDependOnPersistenceOrWebFrameworks()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Dapper", "Microsoft.Data.SqlClient", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Application_ShouldNotDependOnPersistenceOrWebFrameworks()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Dapper", "Microsoft.Data.SqlClient", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void ApiControllers_ShouldOnlyOrchestrateThroughApplication()
    {
        // Los controllers no conocen Infrastructure ni tecnologías de persistencia: solo IDispatcher
        var result = Types.InAssembly(Api.AssemblyReference.Assembly)
            .That().ResideInNamespace("SoccerTournament.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, "Microsoft.EntityFrameworkCore", "Dapper", "Microsoft.Data.SqlClient",
                "SoccerTournament.Application.Abstractions.Persistence")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Domain_ShouldNotReferenceOtherSolutionAssemblies()
    {
        var references = GetSolutionReferences(DomainAssembly);

        Assert.Empty(references);
    }

    [Fact]
    public void Application_ShouldOnlyReferenceDomainAssembly()
    {
        var references = GetSolutionReferences(ApplicationAssembly);

        Assert.All(references, name => Assert.Equal(DomainNamespace, name));
    }

    // Referencias de ensamblado a proyectos de la propia solución
    private static string[] GetSolutionReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => name.StartsWith("SoccerTournament.", StringComparison.Ordinal))
            .ToArray();

    private static void AssertSuccessful(TestResult result)
    {
        var failing = result.FailingTypeNames ?? Array.Empty<string>();
        Assert.True(result.IsSuccessful, $"Tipos que violan la regla: {string.Join(", ", failing)}");
    }
}
