namespace SoccerTournament.Infrastructure.Persistence.Auditing;

/// <summary>
/// Usuario técnico al que se atribuyen las operaciones (created_by / modified_by).
/// Se crea con el script 0003_SystemUser.sql; no puede iniciar sesión.
/// </summary>
internal static class SystemUser
{
    public static readonly Guid Id = new("00000000-0000-0000-0000-000000000001");
}
