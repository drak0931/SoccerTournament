/*
    0003_SystemUser.sql
    Usuario técnico al que se atribuyen created_by / modified_by (la prueba no incluye autenticación).
    No puede iniciar sesión: password_hash no corresponde a ningún hash válido.
    Idempotente.
*/
IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE uuid = '00000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO dbo.users (uuid, username, password_hash, fullname, email, status, created_by)
    VALUES ('00000000-0000-0000-0000-000000000001', N'system', N'!', N'Usuario del sistema', N'system@soccertournament.local', 1, NULL);
END;
GO
