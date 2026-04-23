---
id: MSG-INVENTORY-008
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-INVENTORY-006-DONE
created: 2026-04-18
---

# Visszadobás — HealthCheck BE-09 wiring hiányzik

## Státusz

Az implementáció kiváló, 93/93 teszt zöld. **Egy DoD gate hiányzik:**

**BE-09: `InventoryHealthCheck.cs` worker heartbeat integráció**

## Mit kell csinálni

A tervdok Section 10-ben a teljes kód adott — 1:1 implementáld:

```csharp
public sealed class InventoryHealthCheck : IHealthCheck
{
    private readonly InventoryDbContext _ctx;
    private readonly IWorkerHeartbeatStore _heartbeat;   // ← inject

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        var data = new Dictionary<string, object>();

        try { await _ctx.Database.ExecuteSqlRawAsync("SELECT 1", ct); }
        catch (Exception ex) { return HealthCheckResult.Unhealthy("DB unreachable", ex); }

        var lastRun = await _heartbeat.GetLastTickAsync("inventory-cleanup-worker", ct);
        var workerStatus = lastRun switch
        {
            null => "never",
            _ when DateTimeOffset.UtcNow - lastRun.Value < TimeSpan.FromMinutes(30) => "healthy",
            _ => "stale"
        };

        data["api"] = "Healthy";
        data["cleanupWorker"] = new { lastRun, status = workerStatus };

        return workerStatus == "stale"
            ? HealthCheckResult.Degraded("Cleanup worker heartbeat stale (> 30 min)", data: data)
            : HealthCheckResult.Healthy("All components operational", data);
    }
}
```

`IWorkerHeartbeatStore` már implementálva van (`InMemoryWorkerHeartbeatStore`) — csak a HealthCheck-be kell injektálni és a logikát hozzáadni.

## Elfogadott defer-ek

- **Polly (BE-07)** → Session B, Cutting consumer oldalán implementálandó ✅
- **Grafana dashboard** → külön INFRA task ✅
- **E2E concurrent / rate-limit tesztek** → E2E terminal deploy után ✅

## DoD a javításhoz

- [ ] `InventoryHealthCheck.cs` frissítve: `IWorkerHeartbeatStore` inject + `cleanupWorker.status` logika
- [ ] HealthCheckResult: `healthy` / `stale` / `never` ágak
- [ ] 1–2 unit teszt: `healthy` state, `stale` state (opcionális de ajánlott)
- [ ] `dotnet build` → 0 error · `dotnet test` → ≥93 zöld

---

*Skill: `/spaceos-terminal`*
