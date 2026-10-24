using System.Reflection;
using NetArchTest.Rules;
using SoccerTournament.Application.Abstractions.Messaging;

namespace SoccerTournament.ArchitectureTests;

/// <summary>
/// Valida las reglas de CQRS y Unit of Work en la capa Application:
/// - Los command handlers usan IUnitOfWork y no dependen del lado de lectura.
/// - Los query handlers no dependen del lado de escritura (repositorios EF / IUnitOfWork).
/// </summary>
public class CqrsTests
{
    private const string WriteSideNamespace = "SoccerTournament.Application.Abstractions.Persistence.Write";
    private const string ReadSideNamespace = "SoccerTournament.Application.Abstractions.Persistence.Read";
    private const string UnitOfWorkType = "SoccerTournament.Application.Abstractions.Persistence.Write.IUnitOfWork";

    private static readonly Assembly ApplicationAssembly = Application.AssemblyReference.Assembly;

    [Fact]
    public void CommandHandlers_ShouldUseUnitOfWork()
    {
        var types = Types.InAssembly(ApplicationAssembly).That().HaveNameEndingWith("CommandHandler");
        Assert.NotEmpty(types.GetTypes());

        var result = types.Should().HaveDependencyOn(UnitOfWorkType).GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void CommandHandlers_ShouldNotDependOnReadSide()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("CommandHandler")
            .ShouldNot().HaveDependencyOn(ReadSideNamespace)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void QueryHandlers_ShouldNotDependOnWriteSide()
    {
        var types = Types.InAssembly(ApplicationAssembly).That().HaveNameEndingWith("QueryHandler");
        Assert.NotEmpty(types.GetTypes());

        var result = types.ShouldNot().HaveDependencyOn(WriteSideNamespace).GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void CommandHandlerImplementations_ShouldFollowNamingConvention()
    {
        // Garantiza que las reglas anteriores (basadas en el nombre) cubren todos los handlers reales
        var offenders = GetImplementations(typeof(ICommandHandler<>), typeof(ICommandHandler<,>))
            .Where(type => !type.Name.EndsWith("CommandHandler", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void QueryHandlerImplementations_ShouldFollowNamingConvention()
    {
        var offenders = GetImplementations(typeof(IQueryHandler<,>))
            .Where(type => !type.Name.EndsWith("QueryHandler", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(offenders);
    }

    private static IEnumerable<Type> GetImplementations(params Type[] openGenericContracts) =>
        ApplicationAssembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => type.GetInterfaces().Any(contract =>
                contract.IsGenericType && openGenericContracts.Contains(contract.GetGenericTypeDefinition())));

    private static void AssertSuccessful(TestResult result)
    {
        var failing = result.FailingTypeNames ?? Array.Empty<string>();
        Assert.True(result.IsSuccessful, $"Tipos que violan la regla: {string.Join(", ", failing)}");
    }
}
