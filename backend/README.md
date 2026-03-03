# Mundialito de Fútbol — API

Backend REST para la gestión de torneos de fútbol, construido con **.NET 9** y **Clean Architecture**.

---

## Arquitectura

```
Mundialito.sln
└── src/
    ├── Mundialito.Domain          # Entidades, Value Objects, Domain Events, interfaces de repositorio
    ├── Mundialito.Application     # CQRS (Commands/Queries), Result Pattern, contratos (UoW, repositorios)
    ├── Mundialito.Infrastructure  # EF Core (write), Dapper (read), seed, idempotencia, observabilidad
    └── Mundialito.Api             # Controllers, middlewares, Swagger, DI composition root
```

**Sentido de dependencias (Clean Architecture):**
```
Api → Infrastructure → Application → Domain
                     → Domain
```
> El Domain **no depende** de ninguna otra capa.

---

## Reglas NO NEGOCIABLES

### 1. CQRS Estricto
| Operación | Tecnología |
|-----------|-----------|
| Commands (write) | **EF Core ÚNICAMENTE** |
| Queries (read)   | **Dapper ÚNICAMENTE** |

Prohibido usar EF Core en lecturas. Prohibido usar Dapper en escrituras.

### 2. Unit of Work (UoW)
- UoW **obligatorio** para **todos** los Commands.
- Prohibido llamar a `SaveChanges()` directamente fuera del UoW.

### 3. DELETE idempotente puro
- Siempre devuelve **204 No Content**, exista o no el recurso.

### 4. Error Envelope único
Cualquier error (400 / 404 / 409 / 500) devuelve **exclusivamente**:
```json
{
  "errorCode": "TEAM_NOT_FOUND",
  "message": "The requested team does not exist.",
  "traceId": "<trace-id>"
}
```

### 5. Idempotencia en POST (header `Idempotency-Key`)
| Situación | Respuesta |
|-----------|-----------|
| Mismo key + mismo payload | **200** con el body original (replay) |
| Mismo key + payload distinto | **409** `IDEMPOTENCY_KEY_CONFLICT` |
| Header ausente | **400** `IDEMPOTENCY_KEY_REQUIRED` |

### 6. Paginación / Filtros / Sort
- **Siempre en base de datos** (nunca en memoria).
- Respuesta con envelope estándar:
```json
{ "data": [], "pageNumber": 1, "pageSize": 10, "totalRecords": 50, "totalPages": 5 }
```

### 7. Observabilidad
- Middleware genera/propaga `traceId` y `correlationId` en cada request.
- Logging estructurado con **duración** de request.
- `traceId` incluido en todas las respuestas de error.

### 8. Domain Events (obligatorios y loggeados)
- `TeamCreated`
- `MatchResultRecorded`

### 9. Seed (obligatorio al iniciar con BD vacía)
- **4 Teams**, **5 Players/team**, **6 Matches**, **3 Matches con resultado + goals consistentes**.

### 10. Status Codes cerrados
| Acción | Código |
|--------|--------|
| POST (create) | **201 Created** |
| GET / PUT | **200 OK** |
| DELETE | **204 No Content** (siempre) |
| Validación | **400 Bad Request** |
| No encontrado | **404 Not Found** |
| Conflicto | **409 Conflict** |
| Error interno | **500** (solo desde middleware global) |

---

## Compilar el proyecto

### Requisitos
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9) (`9.0.310` o superior 9.x)
- (Opcional) Docker para la base de datos SQL Server

### Clonar y compilar
```bash
git clone <repo-url>
cd football-tournament-api

# Restaurar dependencias y compilar toda la solución
dotnet build Mundialito.sln
```

### Ejecutar la API
```bash
dotnet run --project src/Mundialito.Api/Mundialito.Api.csproj
```

Con la variable de entorno `ASPNETCORE_ENVIRONMENT=Development`, Swagger UI estará disponible en `http://localhost:<puerto>/`.

### Ejecutar tests
```bash
dotnet test Mundialito.sln
```

---

## Supuestos del Torneo

### Formato
- **Round-robin**: cada equipo juega contra los demás una vez.
- **Puntuación**: Victoria = **3 pts** · Empate = **1 pt** · Derrota = **0 pts**.
- **Desempate** (en ese orden): `points` desc → `goalDifference` desc → `goalsFor` desc.

### Estados de un partido
| Estado | Descripción |
|--------|-------------|
| `Scheduled` | Partido programado, sin resultado. `homeGoals`/`awayGoals` son **null** en la respuesta. |
| `Played`    | Resultado registrado. No puede cambiar de estado de vuelta a Scheduled. |

### Tabla de posiciones (standings)
Calculada en tiempo real por Dapper desde las tablas `MatchResults` y `Matches`.
No existe tabla desnormalizada; los puntos se derivan de los resultados.

---

## Invariantes de Dominio (validadas en Application)

> Las invariantes del **dominio puro** están en las entities.
> Las que requieren consulta a la BD se validan en los **Command Handlers**.

### Team
| Invariante | Error Code |
|------------|-----------|
| Nombre no nulo/vacío | `VALIDATION_ERROR` |
| Nombre único en el sistema | `TEAM_NAME_CONFLICT` |
| No se puede eliminar si tiene Players o Matches | `TEAM_HAS_DEPENDENCIES` |

### Player
| Invariante | Error Code |
|------------|-----------|
| FullName no nulo/vacío | `VALIDATION_ERROR` |
| Number > 0 (si se proporciona) | `VALIDATION_ERROR` |
| TeamId debe existir | `TEAM_NOT_FOUND` |

### Match
| Invariante | Error Code |
|------------|-----------|
| HomeTeamId ≠ AwayTeamId | `VALIDATION_ERROR` |
| Ambos equipos deben existir | `TEAM_NOT_FOUND` |
| No se puede crear si ya existe el mismo enfrentamiento pendiente | `RESOURCE_CONFLICT` |
| Solo puede registrarse resultado si status = `Scheduled` | `MATCH_ALREADY_PLAYED` |

### MatchResult / MatchGoals
| Invariante | Error Code |
|------------|-----------|
| Match debe existir | `MATCH_NOT_FOUND` |
| Match aún en estado Scheduled | `MATCH_ALREADY_PLAYED` |
| Cada PlayerId debe existir | `PLAYER_NOT_FOUND` |
| PlayerId debe pertenecer a homeTeam o awayTeam del match | `PLAYER_NOT_IN_MATCH` |
| Suma de goles de jugadores home == homeGoals **y** suma away == awayGoals | `MATCH_RESULT_INCONSISTENT` |
| HomeGoals ≥ 0 y AwayGoals ≥ 0 | `VALIDATION_ERROR` |
| Goals por jugador > 0 | `VALIDATION_ERROR` |

---

## Catálogo de Error Codes

| Código HTTP | errorCode |
|-------------|-----------|
| 400 | `VALIDATION_ERROR` |
| 400 | `PAGINATION_INVALID` |
| 400 | `IDEMPOTENCY_KEY_REQUIRED` |
| 400 | `MATCH_RESULT_INCONSISTENT` |
| 400 | `PLAYER_NOT_IN_MATCH` |
| 404 | `TEAM_NOT_FOUND` |
| 404 | `PLAYER_NOT_FOUND` |
| 404 | `MATCH_NOT_FOUND` |
| 409 | `TEAM_NAME_CONFLICT` |
| 409 | `MATCH_ALREADY_PLAYED` |
| 409 | `TEAM_HAS_DEPENDENCIES` |
| 409 | `IDEMPOTENCY_KEY_CONFLICT` |
| 409 | `RESOURCE_CONFLICT` |
| 500 | `INTERNAL_ERROR` |

---

## Sprint 6 — Idempotencia: Cómo verificar

La idempotencia está implementada como un **Action Filter** (`IdempotencyFilterAttribute`) aplicado **solo** a los 4 endpoints POST:
- `POST /teams`
- `POST /teams/{teamId}/players`
- `POST /matches`
- `POST /matches/{id}/results`

### Caso 1: Falta el header `Idempotency-Key` → 400

```bash
curl -s -X POST http://localhost:5000/teams \
  -H "Content-Type: application/json" \
  -d '{"name":"Team Alpha"}' | jq .
```

**Respuesta esperada (400):**
```json
{
  "errorCode": "IDEMPOTENCY_KEY_REQUIRED",
  "message": "The 'Idempotency-Key' header is required for this endpoint.",
  "traceId": "<trace-id>"
}
```

---

### Caso 2: Mismo key + mismo payload → REPLAY EXACTO (201, mismo body)

**Primera llamada (crea el equipo):**
```bash
curl -s -X POST http://localhost:5000/teams \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: unique-key-001" \
  -d '{"name":"Team Alpha"}' | jq .
```

**Segunda llamada (mismo key, mismo payload):**
```bash
curl -s -X POST http://localhost:5000/teams \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: unique-key-001" \
  -d '{"name":"Team Alpha"}' | jq .
```

**Respuesta esperada (ambas son idénticas — status 201 + mismo body):**
```json
{
  "id": "...",
  "name": "Team Alpha",
  "createdAt": "..."
}
```
El equipo NO se crea dos veces en la BD. El segundo request devuelve exactamente el mismo status code y body almacenados.

---

### Caso 3: Mismo key + payload distinto → 409 IDEMPOTENCY_KEY_CONFLICT

```bash
curl -s -X POST http://localhost:5000/teams \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: unique-key-001" \
  -d '{"name":"Team DIFERENTE"}' | jq .
```

**Respuesta esperada (409):**
```json
{
  "errorCode": "IDEMPOTENCY_KEY_CONFLICT",
  "message": "An idempotency key conflict was detected: the same key was used with a different request payload.",
  "traceId": "<trace-id>"
}
```

---

### Caso 4: Verificar registro en la BD (opcional)

```bash
# Conectarse al SQL Server y consultar la tabla IdempotencyKeys
SELECT IdempotencyKey, RequestHash, ResponseStatusCode, CreatedAt
FROM   IdempotencyKeys
ORDER  BY CreatedAt DESC;
```

---

### Nota sobre concurrencia (Race Condition)

Si dos requests llegan **simultáneamente** con la misma key y el mismo payload:

1. El primer request inserta el registro → éxito, responde normalmente.
2. El segundo request falla el INSERT (unique constraint violation en `IX_IdempotencyKeys_IdempotencyKey`).
3. El filtro captura el error de constraint, hace un **re-lookup con Dapper**:
   - Si el hash coincide → el response ya fue enviado (idéntico al del primer request).
   - Si el hash difiere → se logea un warning (el cliente ya recibió el response, situación de conflicto en race).
4. **El error SQL NUNCA se expone al cliente.**

El unique index en la columna `IdempotencyKey` garantiza que este manejo es correcto y seguro.

---

## Sprint 7 — Domain Events: Cómo ver en los logs

### Eventos implementados

| Evento | Cuándo se emite | Propiedades loggeadas |
|--------|-----------------|-----------------------|
| `TeamCreatedEvent` | Al crear un equipo (`POST /teams`) | `TeamId`, `TeamName`, `OccurredAt` |
| `MatchResultRecordedEvent` | Al registrar resultado (`POST /matches/{id}/results`) | `MatchId`, `HomeGoals`, `AwayGoals`, `OccurredAt` |

### Arquitectura del flujo

1. **Domain** registra el evento en la entidad (`Team.Create`, `MatchResult.RegisterResultRecordedEvent`).
2. **UnitOfWork.CommitAsync** extrae los eventos del ChangeTracker _antes_ de persistir, limpia la lista _después_ del commit, y loggea con pattern matching tipado.
3. El scope de `TraceId`/`CorrelationId` creado por `ObservabilityMiddleware` fluye automáticamente a todos los loggers del request, incluido `UnitOfWork`.

### Ejemplo de log esperado

Cuando se crea un equipo:
```
info: Mundialito.Infrastructure.Persistence.UnitOfWork[0]
      DomainEvent dispatched: TeamCreatedEvent TeamId=3fa85f64-... TeamName=Team Alpha OccurredAt=2026-03-02T10:30:00Z
      => TraceId: 00-abcdef... CorrelationId: 550e8400-...
```

Cuando se registra un resultado:
```
info: Mundialito.Infrastructure.Persistence.UnitOfWork[0]
      DomainEvent dispatched: MatchResultRecordedEvent MatchId=1a2b3c4d-... HomeGoals=2 AwayGoals=1 OccurredAt=2026-03-02T10:31:00Z
      => TraceId: 00-abcdef... CorrelationId: 550e8400-...
```

### Cómo verificar

```bash
# 1. Crear un equipo y observar logs de la consola
curl -s -X POST http://localhost:5000/teams \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: verify-sprint7-team" \
  -d '{"name":"Sprint 7 Team"}' | jq .

# En la consola del servidor debe aparecer:
# DomainEvent dispatched: TeamCreatedEvent TeamId=... TeamName=Sprint 7 Team OccurredAt=...

# 2. Para filtrar solo los eventos en los logs (Linux/Mac):
dotnet run --project src/Mundialito.Api | grep "DomainEvent dispatched"
```

---

## Sprint 8 — Docker + Seed: Cómo levantar

### Requisitos
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (o Docker Engine + Compose v2)

### Levantar todo con un comando

```bash
docker-compose up --build
```

Esto:
1. Construye la imagen de la API (multi-stage build).
2. Levanta SQL Server 2022 Express.
3. Espera a que SQL Server esté listo (healthcheck).
4. La API aplica migraciones automáticamente al arrancar.
5. La API ejecuta el seed 4/5/6/3 si la BD está vacía.

### Verificar que el seed corrió

En los logs del contenedor `mundialito_api` deben aparecer:

```
DB bootstrap attempt 1/10...
Migrations applied successfully.
Starting database seed (4 teams / 5 players / 6 matches / 3 results)...
Seeded 4 teams.
Seeded 20 players (5 per team).
Seeded 6 matches.
Seed applied successfully. Teams=4 Players=20 Matches=6 (Played=3 Scheduled=3) Results=3.
DB bootstrap complete.
```

### Endpoints con datos inmediatos

```bash
# Tabla de posiciones
curl http://localhost:8080/standings | jq .

# Goleadores
curl http://localhost:8080/scorers | jq .
```

**Tabla esperada:**

| Equipo   | Pts | GF | GA | GD  |
|----------|-----|----|----|-----|
| Team A   |  7  |  3 |  2 | +1  |
| Team B   |  3  |  3 |  5 | -2  |
| Team C   |  1  |  1 |  1 |  0  |
| Team D   |  0  |  0 |  3 | -3  |

**Goleadores esperados:**

| Jugador           | Goles |
|-------------------|-------|
| Team A Player 1   |   3   |
| Team B Player 2   |   2   |
| Team B Player 1   |   1   |
| Team B Player 3   |   1   |
| Team C Player 1   |   1   |

### Detener y limpiar

```bash
docker-compose down          # detiene sin borrar datos
docker-compose down -v       # detiene y borra el volumen (resetea BD)
```
