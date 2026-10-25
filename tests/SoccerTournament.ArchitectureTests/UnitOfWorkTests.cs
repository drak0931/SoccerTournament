using Mono.Cecil;
using Mono.Cecil.Cil;

namespace SoccerTournament.ArchitectureTests;

/// <summary>
/// Regla: no se permite guardar cambios desde el DbContext fuera del Unit of Work.
/// Inspecciona el IL de todos los ensamblados de la solución buscando llamadas a SaveChanges/SaveChangesAsync.
/// </summary>
public class UnitOfWorkTests
{
    private const string AllowedType = "SoccerTournament.Infrastructure.Persistence.UnitOfWork";

    private static readonly string[] SaveMethods = { "SaveChanges", "SaveChangesAsync" };

    [Fact]
    public void SaveChanges_ShouldOnlyBeCalledFromUnitOfWork()
    {
        var assemblies = new[]
        {
            Domain.AssemblyReference.Assembly,
            Application.AssemblyReference.Assembly,
            Infrastructure.AssemblyReference.Assembly,
            Api.AssemblyReference.Assembly
        };

        var callers = new List<string>();

        foreach (var assembly in assemblies)
        {
            using var module = ModuleDefinition.ReadModule(assembly.Location);

            foreach (var type in module.GetTypes())
            {
                foreach (var method in type.Methods.Where(method => method.HasBody))
                {
                    var callsSave = method.Body.Instructions.Any(instruction =>
                        (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                        && instruction.Operand is MethodReference target
                        && SaveMethods.Contains(target.Name)
                        && IsDbContext(target.DeclaringType));

                    // Las lambdas y máquinas de estado async se compilan como tipos anidados del tipo original
                    if (callsSave && GetOutermostType(type).FullName != AllowedType)
                    {
                        callers.Add($"{type.FullName}.{method.Name}");
                    }
                }
            }
        }

        Assert.True(callers.Count == 0, $"SaveChanges invocado fuera del UnitOfWork: {string.Join(", ", callers)}");
    }

    [Fact]
    public void UnitOfWork_ShouldExistAndCallSaveChanges()
    {
        // Evita que la regla anterior pase de forma vacía si se renombra o mueve el UnitOfWork
        using var module = ModuleDefinition.ReadModule(Infrastructure.AssemblyReference.Assembly.Location);

        var unitOfWorkTypes = module.GetTypes().Where(type => GetOutermostType(type).FullName == AllowedType).ToArray();

        Assert.NotEmpty(unitOfWorkTypes);
        Assert.Contains(unitOfWorkTypes.SelectMany(type => type.Methods).Where(method => method.HasBody), method =>
            method.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference target && SaveMethods.Contains(target.Name)));
    }

    private static bool IsDbContext(TypeReference type)
    {
        for (var current = type; current is not null; current = SafeResolve(current)?.BaseType)
        {
            if (current.FullName == "Microsoft.EntityFrameworkCore.DbContext")
            {
                return true;
            }
        }

        return false;
    }

    private static TypeDefinition? SafeResolve(TypeReference type)
    {
        try
        {
            return type.Resolve();
        }
        catch (AssemblyResolutionException)
        {
            return null;
        }
    }

    private static TypeDefinition GetOutermostType(TypeDefinition type)
    {
        while (type.DeclaringType is not null)
        {
            type = type.DeclaringType;
        }

        return type;
    }
}
