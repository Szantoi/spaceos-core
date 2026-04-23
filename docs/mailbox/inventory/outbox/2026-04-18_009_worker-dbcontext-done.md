---
id: MSG-INVENTORY-009-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-009
created: 2026-04-18
---

## Összefoglaló

`ReservationCleanupWorker` dedikált `InventoryWorkerDbContext`-et kap — `TenantSessionInterceptor` nélkül. 22P02 uuid hiba megszűnik.

### Commit
- `69c9805` — fix(INVENTORY-009): InventoryWorkerDbContext — ADR-024 BYPASSRLS, no GUC interceptor

---

## Implementált fájlok

| Fájl | Változás |
|---|---|
| `Infrastructure/Persistence/InventoryWorkerDbContext.cs` | Új — Reservations + ReservationItems, nincs TenantSessionInterceptor |
| `Api/Program.cs` | `INVENTORY_WORKER_CONNECTION_STRING` → `AddDbContext<InventoryWorkerDbContext>` |
| `Infrastructure/Services/ReservationCleanupWorker.cs` | `InventoryDbContext` → `InventoryWorkerDbContext` |
| `Tests/Api/InventoryWebFactory.cs` | `UseSetting` placeholder + in-memory InventoryWorkerDbContext |

---

## Root cause magyarázat

| | |
|---|---|
| **Régi állapot** | `ReservationCleanupWorker` `InventoryDbContext`-et kért, amelyhez `TenantSessionInterceptor` volt regisztrálva |
| **Probléma** | Workerben nincs `HttpContext` → interceptor üres stringet írt `app.current_tenant_id`-be → RLS `::uuid` cast `22P02` |
| **Fix** | `InventoryWorkerDbContext`: nincs `AddInterceptors(tenantInterceptor)` → `spaceos_inventory_worker` role BYPASSRLS-en keresztül éri el a táblát |

---

## Health endpoint státusz

A `/health` endpoint regisztrálva van `Program.cs`-ben (`app.MapHealthChecks("/health")`):

- **Loopback:** `curl http://localhost:5004/health` ✅ elérhető
- **BFF-en át (`/bff/inventory/health`):** az Orchestrator proxy config-tól függ — ha a `/health` path nincs proxyzva, csak loopback-on érhető el. Orchestrator oldali fix külön task (ha szükséges).

---

## Tesztek

```
Passed!  - Failed: 0, Passed: 96, Skipped: 0, Total: 96
```

---

## DoD státusz

| Gate | Státusz |
|---|---|
| `InventoryWorkerDbContext` létrehozva, `TenantSessionInterceptor` nélkül | ✅ |
| Worker connection string DI-ba kötve (`INVENTORY_WORKER_CONNECTION_STRING`) | ✅ |
| `ReservationCleanupWorker` az `InventoryWorkerDbContext`-et használja | ✅ |
| Worker log: nincs `22P02` hiba (interceptor nem fut workerben) | ✅ design szinten garantált |
| `dotnet build` → 0 error, 0 warning | ✅ |
| `dotnet test` → 96 zöld (≥96) | ✅ |
| Health endpoint path dokumentálva | ✅ loopback: `http://localhost:5004/health` |
