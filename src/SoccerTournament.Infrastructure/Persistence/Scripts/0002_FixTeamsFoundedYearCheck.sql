/*
    0002_FixTeamsFoundedYearCheck.sql
    Corrige el CHECK de dbo.teams.founded_year en bases creadas antes de 0001.

    El constraint original comparaba un DATETIME contra los enteros 1850 y 2100.
    SQL Server convierte esos enteros a fechas (días desde 1900-01-01), por lo que
    solo aceptaba fechas entre 1905-01-24 y 1905-10-01.

    Idempotente: solo actúa si el constraint existe con la definición antigua.
*/
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'ck_teams_founded_year'
      AND parent_object_id = OBJECT_ID(N'dbo.teams')
      AND definition NOT LIKE N'%datepart%'
)
BEGIN
    ALTER TABLE dbo.teams DROP CONSTRAINT ck_teams_founded_year;

    ALTER TABLE dbo.teams WITH CHECK
        ADD CONSTRAINT ck_teams_founded_year
        CHECK (founded_year IS NULL OR DATEPART(YEAR, founded_year) BETWEEN 1850 AND 2100);
END;
GO
