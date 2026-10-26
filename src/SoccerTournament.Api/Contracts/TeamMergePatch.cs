using System.Text.Json;
using SoccerTournament.Application.Abstractions;
using SoccerTournament.Application.Features.Teams.Commands;
using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Api.Contracts;

/// <summary>
/// Interpreta un JSON Merge Patch (RFC 7396) para equipos:
/// campo ausente = no modificar; campo con valor = reemplazar; foundedYear: null = limpiar.
/// </summary>
public static class TeamMergePatch
{
    public static Result<PatchTeamCommand> ToCommand(Guid teamId, JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
        {
            return Error.Validation("El cuerpo debe ser un objeto JSON (JSON Merge Patch).", "Patch.InvalidBody");
        }

        var fullName = Optional<string>.Unspecified;
        var shortName = Optional<string>.Unspecified;
        var foundedYear = Optional<DateTime?>.Unspecified;
        var errors = new List<Error>();

        foreach (var property in body.EnumerateObject())
        {
            switch (property.Name.ToLowerInvariant())
            {
                case "fullname" when property.Value.ValueKind == JsonValueKind.String:
                    fullName = Optional<string>.Of(property.Value.GetString()!);
                    break;

                case "shortname" when property.Value.ValueKind == JsonValueKind.String:
                    shortName = Optional<string>.Of(property.Value.GetString()!);
                    break;

                case "foundedyear" when property.Value.ValueKind == JsonValueKind.Null:
                    foundedYear = Optional<DateTime?>.Of(null);
                    break;

                case "foundedyear" when property.Value.ValueKind == JsonValueKind.String && property.Value.TryGetDateTime(out var date):
                    foundedYear = Optional<DateTime?>.Of(date);
                    break;

                case "fullname" or "shortname" or "foundedyear":
                    errors.Add(Error.Validation($"El campo '{property.Name}' tiene un tipo o formato no válido.", property.Name));
                    break;

                default:
                    errors.Add(Error.Validation($"El campo '{property.Name}' no existe o no se puede modificar.", property.Name));
                    break;
            }
        }

        if (errors.Count > 0)
        {
            return new ValidationError(errors);
        }

        return new PatchTeamCommand(teamId, fullName, shortName, foundedYear);
    }
}
