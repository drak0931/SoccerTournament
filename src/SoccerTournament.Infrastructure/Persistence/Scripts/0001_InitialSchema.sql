/*
    0001_InitialSchema.sql
    Esquema inicial del sistema de torneo de fútbol (SQL Server).

    - Idempotente: cada tabla se crea solo si no existe, por lo que puede ejecutarse
      tanto en una BD vacía (Docker) como en la BD local ya existente.
    - Claves primarias GUID NONCLUSTERED con índice CLUSTERED por fecha/orden natural
      para evitar fragmentación por inserciones aleatorias.
    - Fechas en UTC (datetime2(3) + SYSUTCDATETIME()); la conversión a America/El_Salvador se hace en presentación.
    - Ejecutado por DbUp al iniciar la API (journal en dbo.schema_versions).
*/

-- =============================================================
-- users: usuarios del sistema (auditoría created_by / modified_by)
-- =============================================================
IF OBJECT_ID(N'dbo.users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.users
    (
        uuid            UNIQUEIDENTIFIER NOT NULL,
        username        NVARCHAR(50)     NOT NULL,
        password_hash   NVARCHAR(255)    NOT NULL,
        fullname        NVARCHAR(150)    NOT NULL,
        email           NVARCHAR(254)    NOT NULL,
        status          TINYINT          NOT NULL CONSTRAINT df_users_status DEFAULT (1),
        created_at      DATETIME2(3)     NOT NULL CONSTRAINT df_users_created_at DEFAULT (SYSUTCDATETIME()),
        created_by      UNIQUEIDENTIFIER NULL,
        modified_at     DATETIME2(3)     NULL,
        modified_by     UNIQUEIDENTIFIER NULL,
        row_version     ROWVERSION       NOT NULL,

        CONSTRAINT pk_users PRIMARY KEY NONCLUSTERED (uuid),
        CONSTRAINT uq_users_username UNIQUE NONCLUSTERED (username),
        CONSTRAINT uq_users_email UNIQUE NONCLUSTERED (email),
        CONSTRAINT ck_users_status CHECK (status IN (0, 1, 2)),
        CONSTRAINT fk_users_created_by FOREIGN KEY (created_by) REFERENCES dbo.users (uuid),
        CONSTRAINT fk_users_modified_by FOREIGN KEY (modified_by) REFERENCES dbo.users (uuid)
    );

    CREATE CLUSTERED INDEX cix_users_created_at ON dbo.users (created_at);
END;
GO

-- =============================================================
-- teams: equipos participantes
-- =============================================================
IF OBJECT_ID(N'dbo.teams', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.teams
    (
        uuid            UNIQUEIDENTIFIER NOT NULL,
        full_name       NVARCHAR(100)    NOT NULL,
        short_name      NVARCHAR(50)     NOT NULL,
        founded_year    DATETIME         NULL,
        created_at      DATETIME2(3)     NOT NULL CONSTRAINT df_teams_created_at DEFAULT (SYSUTCDATETIME()),
        created_by      UNIQUEIDENTIFIER NOT NULL,
        modified_at     DATETIME2(3)     NULL,
        modified_by     UNIQUEIDENTIFIER NULL,
        row_version     ROWVERSION       NOT NULL,

        CONSTRAINT pk_teams PRIMARY KEY NONCLUSTERED (uuid),
        CONSTRAINT uq_teams_full_name UNIQUE NONCLUSTERED (full_name),
        CONSTRAINT uq_teams_short_name UNIQUE NONCLUSTERED (short_name),
        -- Se valida el año de la fecha (comparar un DATETIME contra enteros convertiría 1850 en una fecha de 1905)
        CONSTRAINT ck_teams_founded_year CHECK (founded_year IS NULL OR DATEPART(YEAR, founded_year) BETWEEN 1850 AND 2100),
        CONSTRAINT fk_teams_created_by FOREIGN KEY (created_by) REFERENCES dbo.users (uuid),
        CONSTRAINT fk_teams_modified_by FOREIGN KEY (modified_by) REFERENCES dbo.users (uuid)
    );

    CREATE CLUSTERED INDEX cix_teams_created_at ON dbo.teams (created_at);
END;
GO

-- =============================================================
-- players: integrantes de cada equipo
-- =============================================================
IF OBJECT_ID(N'dbo.players', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.players
    (
        uuid            UNIQUEIDENTIFIER NOT NULL,
        team_id         UNIQUEIDENTIFIER NOT NULL,
        first_name      NVARCHAR(60)     NULL,
        last_name       NVARCHAR(60)     NOT NULL,
        jersey_number   TINYINT          NOT NULL,
        position        VARCHAR(20)      NOT NULL,
        created_at      DATETIME2(3)     NOT NULL CONSTRAINT df_players_created_at DEFAULT (SYSUTCDATETIME()),
        created_by      UNIQUEIDENTIFIER NOT NULL,
        modified_at     DATETIME2(3)     NULL,
        modified_by     UNIQUEIDENTIFIER NULL,
        row_version     ROWVERSION       NOT NULL,

        CONSTRAINT pk_players PRIMARY KEY NONCLUSTERED (uuid),
        -- Un dorsal por equipo; además agrupa físicamente a los jugadores por equipo
        CONSTRAINT uq_players_team_jersey UNIQUE CLUSTERED (team_id, jersey_number),
        -- Soporta la FK compuesta desde goals (jugador + equipo)
        CONSTRAINT uq_players_uuid_team UNIQUE NONCLUSTERED (uuid, team_id),
        CONSTRAINT ck_players_jersey_number CHECK (jersey_number BETWEEN 1 AND 99),
        CONSTRAINT ck_players_position CHECK (position IN ('Goalkeeper', 'Defender', 'Midfielder', 'Forward')),
        CONSTRAINT fk_players_teams FOREIGN KEY (team_id) REFERENCES dbo.teams (uuid),
        CONSTRAINT fk_players_created_by FOREIGN KEY (created_by) REFERENCES dbo.users (uuid),
        CONSTRAINT fk_players_modified_by FOREIGN KEY (modified_by) REFERENCES dbo.users (uuid)
    );

    CREATE NONCLUSTERED INDEX ix_players_last_name ON dbo.players (last_name, first_name);
END;
GO

-- =============================================================
-- matches: calendario y resultados de partidos
-- =============================================================
IF OBJECT_ID(N'dbo.matches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.matches
    (
        uuid                    UNIQUEIDENTIFIER NOT NULL,
        home_team_id            UNIQUEIDENTIFIER NOT NULL,
        away_team_id            UNIQUEIDENTIFIER NOT NULL,
        scheduled_at            DATETIME2(3)     NOT NULL,
        status                  VARCHAR(20)      NOT NULL CONSTRAINT df_matches_status DEFAULT ('scheduled'),
        home_goals              TINYINT          NULL,
        away_goals              TINYINT          NULL,
        result_registered_at    DATETIME2(3)     NULL,
        created_at              DATETIME2(3)     NOT NULL CONSTRAINT df_matches_created_at DEFAULT (SYSUTCDATETIME()),
        created_by              UNIQUEIDENTIFIER NOT NULL,
        modified_at             DATETIME2(3)     NULL,
        modified_by             UNIQUEIDENTIFIER NULL,
        row_version             ROWVERSION       NOT NULL,

        CONSTRAINT pk_matches PRIMARY KEY NONCLUSTERED (uuid),
        CONSTRAINT ck_matches_different_teams CHECK (home_team_id <> away_team_id),
        CONSTRAINT ck_matches_status CHECK (status IN ('scheduled', 'finished', 'cancelled')),
        -- Un partido finalizado debe tener marcador completo; uno no finalizado no puede tenerlo
        CONSTRAINT ck_matches_result_consistency CHECK (
            (status = 'finished' AND home_goals IS NOT NULL AND away_goals IS NOT NULL AND result_registered_at IS NOT NULL)
            OR
            (status <> 'finished' AND home_goals IS NULL AND away_goals IS NULL AND result_registered_at IS NULL)
        ),
        CONSTRAINT fk_matches_home_team FOREIGN KEY (home_team_id) REFERENCES dbo.teams (uuid),
        CONSTRAINT fk_matches_away_team FOREIGN KEY (away_team_id) REFERENCES dbo.teams (uuid),
        CONSTRAINT fk_matches_created_by FOREIGN KEY (created_by) REFERENCES dbo.users (uuid),
        CONSTRAINT fk_matches_modified_by FOREIGN KEY (modified_by) REFERENCES dbo.users (uuid)
    );

    CREATE CLUSTERED INDEX cix_matches_scheduled_at ON dbo.matches (scheduled_at);
    CREATE NONCLUSTERED INDEX ix_matches_home_team ON dbo.matches (home_team_id, scheduled_at);
    CREATE NONCLUSTERED INDEX ix_matches_away_team ON dbo.matches (away_team_id, scheduled_at);
    CREATE NONCLUSTERED INDEX ix_matches_status ON dbo.matches (status, scheduled_at);
END;
GO

-- =============================================================
-- goals: goles anotados por partido (fuente de la tabla de goleadores)
-- =============================================================
IF OBJECT_ID(N'dbo.goals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.goals
    (
        uuid            UNIQUEIDENTIFIER NOT NULL,
        match_id        UNIQUEIDENTIFIER NOT NULL,
        player_id       UNIQUEIDENTIFIER NOT NULL,
        team_id         UNIQUEIDENTIFIER NOT NULL,
        minute          TINYINT          NOT NULL,
        created_at      DATETIME2(3)     NOT NULL CONSTRAINT df_goals_created_at DEFAULT (SYSUTCDATETIME()),
        created_by      UNIQUEIDENTIFIER NOT NULL,
        modified_at     DATETIME2(3)     NULL,
        modified_by     UNIQUEIDENTIFIER NULL,

        CONSTRAINT pk_goals PRIMARY KEY NONCLUSTERED (uuid),
        CONSTRAINT ck_goals_minute CHECK (minute BETWEEN 1 AND 130),
        CONSTRAINT fk_goals_matches FOREIGN KEY (match_id) REFERENCES dbo.matches (uuid) ON DELETE CASCADE,
        CONSTRAINT fk_goals_teams FOREIGN KEY (team_id) REFERENCES dbo.teams (uuid),
        -- Garantiza que el goleador pertenezca al equipo al que se acredita el gol
        CONSTRAINT fk_goals_player_team FOREIGN KEY (player_id, team_id) REFERENCES dbo.players (uuid, team_id),
        CONSTRAINT fk_goals_created_by FOREIGN KEY (created_by) REFERENCES dbo.users (uuid),
        CONSTRAINT fk_goals_modified_by FOREIGN KEY (modified_by) REFERENCES dbo.users (uuid)
    );

    CREATE CLUSTERED INDEX cix_goals_match_minute ON dbo.goals (match_id, minute);
    CREATE NONCLUSTERED INDEX ix_goals_player ON dbo.goals (player_id) INCLUDE (team_id);
END;
GO

-- =============================================================
-- standings: tabla de posiciones materializada
-- Se actualiza en la misma transacción (UnitOfWork) que registra el resultado del partido.
-- =============================================================
IF OBJECT_ID(N'dbo.standings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.standings
    (
        team_id         UNIQUEIDENTIFIER NOT NULL,
        played          SMALLINT         NOT NULL CONSTRAINT df_standings_played DEFAULT (0),
        won             SMALLINT         NOT NULL CONSTRAINT df_standings_won DEFAULT (0),
        drawn           SMALLINT         NOT NULL CONSTRAINT df_standings_drawn DEFAULT (0),
        lost            SMALLINT         NOT NULL CONSTRAINT df_standings_lost DEFAULT (0),
        goals_for       SMALLINT         NOT NULL CONSTRAINT df_standings_goals_for DEFAULT (0),
        goals_against   SMALLINT         NOT NULL CONSTRAINT df_standings_goals_against DEFAULT (0),
        goal_difference SMALLINT         NOT NULL CONSTRAINT df_standings_goal_difference DEFAULT (0),
        points          SMALLINT         NOT NULL CONSTRAINT df_standings_points DEFAULT (0),
        modified_at     DATETIME2(3)     NULL,
        row_version     ROWVERSION       NOT NULL,

        CONSTRAINT pk_standings PRIMARY KEY CLUSTERED (team_id),
        CONSTRAINT ck_standings_played CHECK (played = won + drawn + lost),
        CONSTRAINT ck_standings_goal_difference CHECK (goal_difference = goals_for - goals_against),
        CONSTRAINT fk_standings_teams FOREIGN KEY (team_id) REFERENCES dbo.teams (uuid) ON DELETE CASCADE
    );

    -- Orden oficial: puntos, diferencia de gol, goles a favor
    CREATE NONCLUSTERED INDEX ix_standings_ranking ON dbo.standings (points DESC, goal_difference DESC, goals_for DESC);
END;
GO

-- =============================================================
-- domain_events: registro de eventos de dominio con trazabilidad
-- =============================================================
IF OBJECT_ID(N'dbo.domain_events', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.domain_events
    (
        uuid            UNIQUEIDENTIFIER NOT NULL,
        event_type      NVARCHAR(200)    NOT NULL,
        aggregate_type  NVARCHAR(100)    NOT NULL,
        aggregate_id    UNIQUEIDENTIFIER NOT NULL,
        payload         NVARCHAR(MAX)    NOT NULL,
        occurred_at     DATETIME2(3)     NOT NULL,
        correlation_id  NVARCHAR(64)     NULL,
        trace_id        VARCHAR(32)      NULL,

        CONSTRAINT pk_domain_events PRIMARY KEY NONCLUSTERED (uuid),
        CONSTRAINT ck_domain_events_payload CHECK (ISJSON(payload) = 1)
    );

    CREATE CLUSTERED INDEX cix_domain_events_occurred_at ON dbo.domain_events (occurred_at);
    CREATE NONCLUSTERED INDEX ix_domain_events_aggregate ON dbo.domain_events (aggregate_type, aggregate_id);
    CREATE NONCLUSTERED INDEX ix_domain_events_trace_id ON dbo.domain_events (trace_id);
END;
GO

-- =============================================================
-- idempotency_records: almacenamiento de Idempotency-Key para POST
-- =============================================================
IF OBJECT_ID(N'dbo.idempotency_records', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.idempotency_records
    (
        idempotency_key         NVARCHAR(100)   NOT NULL,
        request_method          VARCHAR(10)     NOT NULL,
        request_path            NVARCHAR(300)   NOT NULL,
        request_hash            CHAR(64)        NOT NULL,
        status                  VARCHAR(20)     NOT NULL,
        response_status_code    SMALLINT        NULL,
        response_body           NVARCHAR(MAX)   NULL,
        response_location       NVARCHAR(500)   NULL,
        created_at              DATETIME2(3)    NOT NULL CONSTRAINT df_idempotency_created_at DEFAULT (SYSUTCDATETIME()),
        completed_at            DATETIME2(3)    NULL,
        expires_at              DATETIME2(3)    NOT NULL,

        CONSTRAINT pk_idempotency_records PRIMARY KEY CLUSTERED (idempotency_key),
        CONSTRAINT ck_idempotency_status CHECK (status IN ('in_progress', 'completed')),
        -- Solo un registro completado guarda la respuesta original
        CONSTRAINT ck_idempotency_completed CHECK (
            (status = 'completed' AND response_status_code IS NOT NULL)
            OR
            (status = 'in_progress' AND response_status_code IS NULL)
        )
    );

    CREATE NONCLUSTERED INDEX ix_idempotency_records_expires_at ON dbo.idempotency_records (expires_at);
END;
GO
