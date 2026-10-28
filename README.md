# Soccer Tournament

Sistema para el control de un torneo de fútbol: equipos, integrantes, calendario de partidos, registro de resultados, tabla de posiciones y goleadores.

**Backend** .NET 8 (Clean Architecture + CQRS) · **Frontend** Next.js 15 · **BD** SQL Server 2022 · **Observabilidad** Serilog + OpenTelemetry + Jaeger · **Docker Compose**

> Diagramas de arquitectura: [`docs/architecture.md`](docs/architecture.md)

---

## Inicio rápido

Requisitos: Docker con al menos 2 GB de memoria asignada.

```bash
docker-compose up --build
```

| Servicio | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API (Swagger) | http://localhost:8080/swagger |
| Jaeger (trazas) | http://localhost:16686 |
| SQL Server | `localhost,14330` · usuario `sa` |

Al arrancar, la API crea la base, aplica los scripts versionados y carga el **seed** (4 equipos, 5 jugadores por equipo, 6 partidos, 3 resultados). Cuando el servicio `api` queda *healthy*, la tabla de posiciones ya tiene datos.

- Contraseña de SQL Server: variable `MSSQL_SA_PASSWORD` en un archivo `.env` (plantilla en [`.env.example`](.env.example)). El valor por defecto del `docker-compose.yml` existe solo para que el entorno de evaluación levante sin pasos previos; no debe usarse en otro entorno.
- Reiniciar desde cero (borra el volumen y vuelve a ejecutar el seed): `docker-compose down -v`

---

## Desarrollo local (sin Docker)

**API** (requiere SDK .NET 8 y SQL Server / LocalDB):

```bash
dotnet user-secrets set "ConnectionStrings:SoccerTournament" "Server=(localdb)\MSSQLLocalDB;Database=SoccerTournament;Integrated Security=True;Encrypt=False" --project src/SoccerTournament.Api
dotnet run --project src/SoccerTournament.Api          # http://localhost:5019/swagger
```

Opcional, trazas en Jaeger: `dotnet user-secrets set "Observability:OtlpEndpoint" "http://localhost:4317" --project src/SoccerTournament.Api`

**Frontend** (requiere Node 20+):

```bash
cd frontend
npm install
npm run dev                                             # http://localhost:3000
```

Por defecto usa la API en `http://localhost:5019` (configurable con `API_BASE_URL`, ver `frontend/.env.example`).

**Pruebas manuales de la API:** [`src/SoccerTournament.Api/SoccerTournament.Api.http`](src/SoccerTournament.Api/SoccerTournament.Api.http)

---

## Pruebas

```bash
dotnet test                       # 189 pruebas backend
cd frontend && npm test           # 24 pruebas frontend
```

| Proyecto | Pruebas | Qué cubre |
|---|---|---|
| `SoccerTournament.Domain.Tests` | 48 | Result pattern, invariantes de agregados, transiciones de estado, eventos, cálculo de posiciones |
| `SoccerTournament.Application.Tests` | 30 | Dispatcher y pipelines, handlers, Unit of Work en commands |
| `SoccerTournament.Infrastructure.Tests` | 61 (+6 opcionales) | Modelo EF, change tracking, auditoría, errores SQL, SQL de Dapper, orden por defecto, seed, contenedor DI, trazas de arranque |
| `SoccerTournament.Api.Tests` | 35 | HTTP de punta a punta (WebApplicationFactory): verbos, códigos, ProblemDetails, idempotencia, CorrelationId/TraceId, logs, health check |
| `SoccerTournament.ArchitectureTests` | 15 | Dependencias entre capas, separación CQRS, UoW en todos los commands, `SaveChanges` solo en el UoW (análisis de IL) |
| `frontend` (Vitest) | 24 | Cliente HTTP y errores, fechas en hora de El Salvador, parámetros de URL, validación del resultado |

Las 6 pruebas opcionales ejecutan el SQL real de Dapper contra una BD (solo `SELECT`): defina `SOCCERTOURNAMENT_TEST_DB` con una cadena de conexión.

---

## Estructura

```
src/
├─ SoccerTournament.Domain/          Agregados, Result pattern, eventos de dominio (sin dependencias)
├─ SoccerTournament.Application/     CQRS: dispatcher propio, commands, queries, validaciones, contratos
├─ SoccerTournament.Infrastructure/  EF Core (write), Dapper (read), Unit of Work, idempotencia, DbUp, seed
└─ SoccerTournament.Api/             Controllers, idempotencia HTTP, observabilidad, Dockerfile
tests/                               Pruebas por capa + arquitectura
frontend/                            Next.js (core · infrastructure · services · components · features · app)
docs/architecture.md                 Diagramas
docker-compose.yml                   db · api · frontend · jaeger
```

---

## API

| Verbo | Ruta | Éxito | Errores |
|---|---|---|---|
| GET | `/api/teams` | 200 | 400 |
| GET | `/api/teams/{id}` | 200 | 404 |
| POST | `/api/teams` *(Idempotency-Key)* | 201 + Location | 400 · 409 |
| PUT | `/api/teams/{id}` | 200 | 400 · 404 · 409 |
| PATCH | `/api/teams/{id}` *(JSON Merge Patch)* | 200 | 400 · 404 · 409 |
| DELETE | `/api/teams/{id}` | 204 (idempotente) | 409 |
| GET | `/api/teams/{id}/players` | 200 | 400 · 404 |
| POST | `/api/teams/{id}/players` *(Idempotency-Key)* | 201 + Location | 400 · 404 · 409 |
| GET | `/api/players`, `/api/players/{id}` | 200 | 400 · 404 |
| GET | `/api/matches`, `/api/matches/{id}` | 200 | 400 · 404 |
| POST | `/api/matches` *(Idempotency-Key)* | 201 + Location | 400 · 404 |
| POST | `/api/matches/{id}/result` *(Idempotency-Key)* | 201 + Location | 400 · 404 · 409 |
| PATCH | `/api/matches/{id}` (`{"status":"cancelled"}`) | 204 | 400 · 404 · 409 |
| GET | `/api/standings` | 200 | 400 |
| GET | `/api/scorers` | 200 | 400 |
| GET | `/health` | 200 Healthy | 503 Unhealthy (SQL Server no responde) |

**Listados:** `pageNumber`, `pageSize` (máx. 100), `sortBy` (lista blanca por recurso), `sortDirection` (`asc`/`desc`) y filtros por entidad. Si se omite `sortDirection`, cada campo usa su orden natural: puntos, diferencia de gol, goles a favor y goles de mayor a menor; nombres y fechas de A a Z o de la más antigua a la más reciente. Respuesta:

```json
{ "data": [], "pageNumber": 1, "pageSize": 10, "totalRecords": 50, "totalPages": 5 }
```

**Errores:** `application/problem+json` con `errorCode`, `traceId` y `errors` por campo.

**Idempotencia (POST):** `Idempotency-Key` obligatorio. La misma clave con el mismo cuerpo devuelve la respuesta original (status, body y Location) con `Idempotency-Replayed: true`, sin volver a ejecutar el command. Con otro cuerpo, o si la solicitud original sigue en proceso, responde 409. Las claves expiran a las 24 h.

---

## Matriz de cumplimiento

| Requisito | Dónde se cumple |
|---|---|
| **Clean Architecture** (Domain, Application, Infrastructure, Api) | `src/*` · referencias de proyecto · `tests/SoccerTournament.ArchitectureTests/LayerDependencyTests.cs` |
| **CQRS — commands con EF Core** | `Application/Features/*/Commands` · `Infrastructure/Persistence/Repositories` |
| **CQRS — queries solo con Dapper, paginación en BD** | `Application/Features/*/Queries` · `Infrastructure/ReadSide/*ReadService.cs` · `ReadSide/Sql/PagedQuery.cs` |
| Separación verificada | `ArchitectureTests/CqrsTests.cs` |
| **Unit of Work** (transacciones, commit explícito, varios repositorios, todos los commands) | `Infrastructure/Persistence/UnitOfWork.cs` · `CqrsTests.CommandHandlers_ShouldUseUnitOfWork` · `UnitOfWorkTests` (IL) |
| **Result pattern** (Success, Failure, mensaje, código opcional) | `Domain/Abstractions/Result.cs`, `Error.cs` |
| Mapeo 200/201/400/404/409 | `Api/Common/ApiControllerBase.cs` · `Api/Common/ErrorHttpMapping.cs` |
| **REST** (GET, POST, PUT, PATCH, DELETE idempotente) | `Api/Controllers/*` · `Api/Contracts/TeamMergePatch.cs` |
| **Idempotencia en POST** (header, persistencia, sin duplicados, misma respuesta) | `Api/Idempotency/IdempotencyFilter.cs` · `Infrastructure/Idempotency/SqlIdempotencyStore.cs` · tabla `idempotency_records` |
| **Logging estructurado** (Serilog, niveles, sin datos sensibles) | `Api/Observability/*` · `appsettings.json` (`Serilog`) · `Application/Abstractions/Behaviors/LoggingBehavior.cs` |
| **Trazabilidad** (CorrelationId, TraceId, duración, logs enlazados) | `Api/Observability/CorrelationIdMiddleware.cs`, `RequestLogging.cs`, `ActivityEnricher.cs` |
| OpenTelemetry + Jaeger *(opcional)* | `Api/Observability/ObservabilityExtensions.cs` · servicio `jaeger` en `docker-compose.yml` |
| **Eventos de dominio** (al crear y al registrar resultado; registrados, loggeados, trazables) | `Domain/*/Events` · `UnitOfWork` → `domain_events` · `Application/Features/Events/DomainEventLogHandlers.cs` |
| **Equipos**: CRUD, filtros, paginación, orden dinámico | `TeamsController` · `TeamReadService` |
| **Jugadores**: registro por equipo, filtros, paginación | `POST /api/teams/{id}/players` · `PlayerReadService` |
| **Partidos**: creación, resultado, filtros por fecha/equipo/estado | `MatchesController` · `MatchReadService` |
| **Tabla automática** (puntos → DG → GF) | `Domain/Standings/Standing.cs` · `StandingReadService.cs` (`ROW_NUMBER`) |
| **Goleadores** | `ScorerReadService.cs` (`GROUP BY` + `DENSE_RANK`) |
| **Seed** automático si la BD está vacía, en Infrastructure, con Docker | `Infrastructure/Persistence/Seeding/*` · `DatabaseInitializer.cs` |
| **Filtros y paginación** en todos los listados | `Application/Abstractions/Pagination/*` · `PagedQuery.cs` |
| **Frontend Next.js** | `frontend/src/*` |
| **Docker** | `src/SoccerTournament.Api/Dockerfile` · `frontend/Dockerfile` · `docker-compose.yml` |
| **Diagrama de arquitectura** | `docs/architecture.md` |

---

## Decisiones de diseño

| Decisión | Motivo |
|---|---|
| **Dispatcher propio** en lugar de MediatR | Separa en tipos distintos commands y queries; los pipelines de escritura nunca se aplican a lecturas; sin dependencia de licencia comercial |
| **Esquema gobernado por scripts SQL (DbUp)**, no por migraciones de EF | El esquema usa constraints, índices clúster y CHECKs que se expresan con más precisión en SQL; EF solo mapea |
| **PK GUID no clúster + índice clúster por fecha** | Evita fragmentación por inserciones aleatorias |
| **Tabla de posiciones materializada** | Se actualiza en la misma transacción que el resultado: demuestra consistencia entre repositorios en un Unit of Work y la lectura es O(equipos) |
| `UnitOfWork.CommitAsync` devuelve `Result` | Las violaciones de unicidad o concurrencia por condiciones de carrera llegan como 409, sin propagar excepciones |
| **DELETE devuelve 204 aunque el equipo ya no exista** | El estado final pedido (inexistente) se cumple: idempotencia real |
| Idempotencia en **transacción propia** (Dapper), separada del Unit of Work | Es deduplicación de requests HTTP, no un caso de uso; permite reclamar la clave antes y guardar la respuesta después |
| Frontend consume la API **solo desde el servidor** de Next.js | La API no se expone al navegador y no requiere CORS |
| Fechas en **UTC** en API y BD; la UI muestra hora de **El Salvador** | Un único instante canónico; formato dd/MM/yyyy en presentación |
| Auditoría con **usuario técnico** `system` | La prueba no incluye autenticación; `created_by` es obligatorio en el esquema |

---

## Limitaciones conocidas

| Tema | Comportamiento | Motivo / alternativa |
|---|---|---|
| Idempotencia ante una caída del proceso | Si la API se detiene justo después de confirmar el command y antes de guardar la respuesta, la clave queda `in_progress` hasta expirar (24 h): los reintentos con esa clave reciben 409 | Liberarla automáticamente podría re-ejecutar un command ya confirmado (por ejemplo, programar dos veces el mismo partido). La alternativa completa es guardar la respuesta dentro de la misma transacción del Unit of Work |
| Alcance de la `Idempotency-Key` | Única en todo el sistema, no por cliente | La prueba no incluye autenticación, por lo que no hay identidad de cliente. Reutilizar una clave en otra ruta o con otro cuerpo responde 409 |
| Swagger en Docker | Habilitado con `Swagger__Enabled=true` en `docker-compose.yml` | Intencional para la evaluación; en un ambiente real se deshabilita |
| Conexión como `sa` en Docker | La API usa el usuario `sa` del contenedor | Contenedor desechable de evaluación; en un ambiente real se crea un login con permisos mínimos |
| `dateTo` sin hora | Una fecha exacta a medianoche (`2026-10-31` o `2026-10-31T00:00:00`) incluye el día completo | El frontend siempre envía el final del día local (`23:59:59`), por lo que no se ve afectado |
| Goleadores en el formulario de resultado | El frontend carga hasta 100 jugadores por equipo | Suficiente para una plantilla de fútbol |

---

## Observabilidad

- **Logs**: JSON en stdout (`docker-compose logs api`), cada línea con `TraceId`, `SpanId` y `CorrelationId`.
- **Seguimiento de un error**: el `traceId` del ProblemDetails (o el header `X-Trace-Id`) se busca en Jaeger, en los logs y en `domain_events.trace_id`.
- **Jaeger**: servicio `SoccerTournament.Api` con spans HTTP → Command/Query → UnitOfWork.Commit → SQL → DomainEvent, más la traza `DatabaseInitialization` del arranque.
