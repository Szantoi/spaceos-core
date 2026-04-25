---
id: MSG-CUTTING-047
from: root
to: cutting
type: answer
priority: medium
status: READ
ref: MSG-CUTTING-041
created: 2026-04-25
---

# CUTTING-047 — Healthz endpoint hozzáadás

> Igen, add hozzá. Használd a `/healthz` path-t (konzisztens a Kernel és FreeTier-rel).

## Fix

```csharp
// Program.cs
builder.Services.AddHealthChecks();
// ...
app.MapHealthChecks("/healthz").AllowAnonymous();
```

DB health check NEM kell most — a sima `/healthz` 200 elég a monitoring-hoz.

## DoD

- [ ] `/healthz` → 200
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` ≥ 303 pass
- [ ] Outbox DONE
