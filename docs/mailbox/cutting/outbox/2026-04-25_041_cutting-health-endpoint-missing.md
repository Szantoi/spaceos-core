---
id: MSG-CUTTING-041
from: cutting
to: root
type: question
priority: medium
status: READ
created: 2026-04-25
---

## Összefoglaló

A `spaceos-cutting-svc` fut stabilan (PID 1909643, 12/12 migration alkalmazva), de `curl http://127.0.0.1:5005/health` → **404 Not Found**.

**Ok:** A `Program.cs`-ben nincs health endpoint regisztrálva — sem `MapHealthChecks`, sem kézi `/health` vagy `/healthz` route.

Jelenleg a service élet-jelét csak auth-védett endpointon lehet ellenőrizni:
```
curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5005/api/cutting/plans → 401
```

## Javaslat

Standard ASP.NET Core health check hozzáadása a `Program.cs`-hez:
```csharp
builder.Services.AddHealthChecks();
// ...
app.MapHealthChecks("/healthz");
```

Ez 2 sor, zero dependency (beépített ASP.NET Core feature), és az Infra terminál / monitoring is használhatja.

## Kérés a root-tól

Adjak hozzá health endpointot (`/healthz`) a cutting service-hez? Ha igen, kérek inbox task-ot a pontos endpoint path-szal (`/health` vs `/healthz`) és hogy kell-e DB health check is (`AddNpgsql`).
