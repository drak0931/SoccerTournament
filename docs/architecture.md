# Arquitectura — Soccer Tournament

Diagramas en [Mermaid](https://mermaid.js.org/) (GitHub los dibuja directamente).

1. [Despliegue con Docker](#1-despliegue-con-docker)
2. [Capas (Clean Architecture)](#2-capas-clean-architecture)
3. [CQRS: escritura y lectura](#3-cqrs-escritura-con-ef-core-lectura-con-dapper)
4. [Flujo de un command: idempotencia, Unit of Work, eventos y trazas](#4-flujo-completo-de-un-command)
5. [Observabilidad: logs y trace flow](#5-observabilidad-logs-y-trace-flow)

---

## 1. Despliegue con Docker

```mermaid
flowchart LR
    user(["👤 Usuario<br/>navegador"])

    subgraph compose["docker-compose · red soccer-tournament_default"]
        direction LR
        fe["<b>frontend</b><br/>Next.js 15 standalone<br/>Node 22 · :3000"]
        api["<b>api</b><br/>ASP.NET Core 8<br/>:8080 · /health · /swagger"]
        db[("<b>db</b><br/>SQL Server 2022<br/>volumen mssql-data")]
        jaeger["<b>jaeger</b><br/>Jaeger 2 · UI :16686<br/>OTLP :4317"]
    end

    user -- "HTTP :3000" --> fe
    user -. "Swagger :8080" .-> api
    user -. "UI :16686" .-> jaeger
    fe -- "HTTP interno<br/>http://api:8080<br/>(Server Components / Server Actions)" --> api
    api -- "TDS :1433<br/>EF Core · Dapper · DbUp" --> db
    api -- "OTLP gRPC :4317<br/>spans" --> jaeger

    db -. "healthy" .-> api
    api -. "healthy<br/>(scripts + seed listos)" .-> fe
```

Orden de arranque garantizado por healthchecks: **db → api → frontend**.
La API ejecuta los scripts versionados (DbUp) y el **seed** antes de escuchar; cuando queda *healthy*, la tabla de posiciones ya se puede consultar.
Todos los servicios usan `restart: unless-stopped`.

---

## 2. Capas (Clean Architecture)

```mermaid
flowchart TB
    subgraph api["<b>Api</b> — solo orquesta"]
        ctrl["Controllers<br/>(request → command/query)"]
        idem["IdempotencyFilter<br/>[Idempotent]"]
        mw["CorrelationIdMiddleware<br/>Serilog request logging<br/>GlobalExceptionHandler"]
        map["ApiControllerBase<br/>Result → 200/201/204/400/404/409"]
    end

    subgraph app["<b>Application</b> — casos de uso"]
        disp["Dispatcher propio<br/>pipelines Logging → Validation"]
        cmd["Command handlers"]
        qry["Query handlers"]
        contracts["Contratos<br/>Persistence.Write: IUnitOfWork, I*Repository<br/>Persistence.Read: I*ReadService<br/>IIdempotencyStore · IDateTimeProvider"]
        evh["Domain event handlers (logs)"]
    end

    subgraph domain["<b>Domain</b> — sin dependencias"]
        agg["Agregados: Team, Player, Match(+Goal), Standing"]
        res["Result · Result&lt;T&gt; · Error"]
        evt["Eventos de dominio"]
    end

    subgraph infra["<b>Infrastructure</b> — implementa contratos"]
        ef["EF Core: ApplicationDbContext<br/>Repositorios · UnitOfWork"]
        dap["Dapper: *ReadService"]
        store["SqlIdempotencyStore"]
        dbup["DbUp + DatabaseSeeder"]
    end

    api --> app
    app --> domain
    infra --> app
    infra --> domain
    api -. "solo registro DI<br/>(composition root)" .-> infra
```

| Regla | Cómo se garantiza |
|---|---|
| Domain no depende de ninguna capa | Sin referencias de proyecto + `LayerDependencyTests` |
| Application depende solo de Domain | Referencia única + prueba de ensamblados |
| Infrastructure implementa contratos | Interfaces en `Application.Abstractions` |
| Api solo orquesta | Controllers solo usan `IDispatcher` (`ApiControllers_ShouldOnlyOrchestrateThroughApplication`) |

---

## 3. CQRS: escritura con EF Core, lectura con Dapper

```mermaid
flowchart LR
    ctrl["Controller"] --> disp{{"IDispatcher"}}

    subgraph write["WRITE SIDE — Commands"]
        direction TB
        cp["Pipeline: Logging → Validation"]
        ch["CommandHandler"]
        repo["Repositorios EF Core<br/>(registran cambios, nunca guardan)"]
        agg["Agregados de dominio<br/>reglas + eventos"]
        uow["<b>IUnitOfWork.CommitAsync()</b><br/>BEGIN TRAN<br/>SaveChanges (entidades + domain_events)<br/>COMMIT / ROLLBACK<br/>errores SQL → Result 409"]
        cp --> ch --> repo --> agg
        ch --> uow
    end

    subgraph read["READ SIDE — Queries"]
        direction TB
        qp["Pipeline: Logging → Validation<br/>(sortBy en lista blanca)"]
        qh["QueryHandler"]
        rs["*ReadService (Dapper)<br/>SQL parametrizado · WHERE dinámico<br/>ORDER BY de lista blanca<br/>OFFSET/FETCH + COUNT en 1 viaje"]
        qp --> qh --> rs
    end

    disp -- "SendAsync(ICommand)" --> cp
    disp -- "QueryAsync(IQuery)" --> qp

    uow --> sql[("SQL Server")]
    rs --> sql
```

- **Commands** solo conocen `Persistence.Write`; **queries** solo `Persistence.Read`. `CqrsTests` lo verifica, junto con que **todo** command handler usa `IUnitOfWork`.
- `SaveChanges` solo existe dentro de `UnitOfWork`; `UnitOfWorkTests` inspecciona el IL compilado de toda la solución.
- La tabla de posiciones es una proyección materializada (`standings`) que el command `RegisterMatchResult` actualiza **en la misma transacción** que el partido y sus goles. La query la ordena con `ROW_NUMBER()` por puntos → diferencia de gol → goles a favor.

---

## 4. Flujo completo de un command

Registrar un resultado: `POST /api/matches/{id}/result`

```mermaid
sequenceDiagram
    autonumber
    actor U as Usuario
    participant FE as Frontend (Server Action)
    participant MW as CorrelationId + Request log
    participant IF as IdempotencyFilter
    participant IS as idempotency_records
    participant D as Dispatcher (Logging → Validation)
    participant H as RegisterMatchResultHandler
    participant R as Repositorios EF
    participant UoW as UnitOfWork
    participant DB as SQL Server
    participant EH as Domain event handlers

    U->>FE: Envía marcador y goleadores
    FE->>MW: POST + Idempotency-Key + X-Correlation-Id
    MW->>MW: TraceId (W3C) + CorrelationId → LogContext / baggage
    MW->>IF: request
    IF->>IS: Reclamo atómico de la clave (UPDLOCK, HOLDLOCK)
    alt Clave ya completada con el mismo payload
        IS-->>IF: respuesta guardada
        IF-->>FE: misma respuesta original + Idempotency-Replayed: true
    else Misma clave, otro payload o en proceso
        IF-->>FE: 409 Conflict
    else Clave nueva
        IF->>D: SendAsync(RegisterMatchResultCommand)
        D->>H: (validado)
        H->>R: Match + Goals, Players, Standings
        H->>H: match.RegisterResult() → MatchResultRegisteredEvent<br/>standing.ApplyResult() local y visitante
        H->>UoW: CommitAsync()
        UoW->>DB: BEGIN TRAN · UPDATE matches · INSERT goals<br/>UPDATE standings x2 · INSERT domain_events (trace_id, correlation_id) · COMMIT
        UoW->>EH: Despacha eventos tras el commit (log estructurado)
        UoW-->>H: Result.Success
        H-->>IF: 201 Created + Location
        IF->>IS: Guarda status, body y Location
        IF-->>FE: 201 Created
    end
    MW->>MW: Log "HTTP POST … respondió 201 en N ms"
    FE-->>U: Detalle actualizado (tabla y goleadores revalidados)
```

---

## 5. Observabilidad: logs y trace flow

```mermaid
flowchart LR
    req["Request HTTP"] --> corr["CorrelationIdMiddleware<br/>X-Correlation-Id (lee o genera)<br/>X-Trace-Id en la respuesta"]

    corr --> span1["Span: GET/POST api/…<br/>(ASP.NET Core)"]
    span1 --> span2["Span: Command X / Query Y<br/>(Application)"]
    span2 --> span3["Span: UnitOfWork.Commit"]
    span2 --> span4["Span: SQL<br/>(SqlClient: EF Core y Dapper)"]
    span3 --> span5["Span: DomainEvent X"]

    span1 & span2 & span3 & span4 & span5 -- "OTLP" --> jaeger[("Jaeger")]

    corr -. "TraceId · SpanId · CorrelationId" .-> logs["Serilog → JSON en stdout<br/>niveles: Debug · Information · Warning · Error"]
    span3 -. "trace_id · correlation_id" .-> de[("domain_events")]
    corr -. "traceId" .-> pd["ProblemDetails de error"]

    start["Arranque: DatabaseInitialization<br/>(scripts + seed, traza raíz propia)"] -- "OTLP" --> jaeger
    start -. "correlation_id = startup-…" .-> de
```

Un mismo **TraceId** enlaza: header `X-Trace-Id` ↔ `traceId` del ProblemDetails ↔ logs JSON ↔ `domain_events.trace_id` ↔ traza en Jaeger.
Los logs no incluyen cuerpos, query strings, IPs ni cadenas de conexión.
