---
id: MSG-INVENTORY-008-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-008
created: 2026-04-18
---

## Összefoglaló

BE-09 HealthCheck wiring implementálva. Az `InventoryHealthCheck.cs` létrehozva és regisztrálva.

### Commit
- `97aaa9f` — feat(INVENTORY-008): InventoryHealthCheck BE-09 — worker heartbeat wiring

---

## Implementált fájlok

| Fájl | Tartalom |
|---|---|
| `Infrastructure/HealthChecks/InventoryHealthCheck.cs` | IHealthCheck implementáció — DB ping + worker heartbeat status |
| `Infrastructure/Extensions/ServiceCollectionExtensions.cs` | `AddHealthChecks().AddCheck<InventoryHealthCheck>("inventory")` regisztráció |
| `Api/Program.cs` | `app.MapHealthChecks("/health")` endpoint |
| `Tests/Infrastructure/InventoryHealthCheckTests.cs` | 3 unit teszt: healthy / stale / never állapotok |
| `Tests/SpaceOS.Modules.Inventory.Tests.csproj` | `Microsoft.EntityFrameworkCore.Sqlite 8.0.11` — SQLite in-memory raw SQL supporthoz |

---

## Tesztek

```
Passed!  - Failed: 0, Passed: 96, Skipped: 0, Total: 96
```

**3 új teszt** (96 total, volt 93):
- `CheckHealthAsync_WhenWorkerHeartbeatRecent_ReturnsHealthy` — lastRun -5 min → Healthy
- `CheckHealthAsync_WhenWorkerHeartbeatStale_ReturnsDegraded` — lastRun -45 min → Degraded + "stale"
- `CheckHealthAsync_WhenWorkerNeverRan_ReturnsHealthy` — lastRun null → Healthy ("never")

---

## DoD státusz

| Gate | Státusz |
|---|---|
| `InventoryHealthCheck.cs` frissítve: `IWorkerHeartbeatStore` inject + `cleanupWorker.status` logika | ✅ |
| HealthCheckResult: `healthy` / `stale` / `never` ágak | ✅ |
| 3 unit teszt: `healthy`, `stale`, `never` state | ✅ |
| `dotnet build` → 0 error, 0 warning | ✅ |
| `dotnet test` → 96 zöld (≥93) | ✅ |
