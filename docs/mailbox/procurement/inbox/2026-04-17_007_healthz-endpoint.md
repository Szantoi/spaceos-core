---
id: MSG-PROCUREMENT-007
from: root
to: procurement
type: task
priority: high
status: READ
ref: SPRINT4
created: 2026-04-17
---

# PROCUREMENT-007 — /healthz endpoint

## Kontextus

Minden más SpaceOS service-nek van `/healthz` AllowAnonymous health endpoint-ja,
a Procurement-nek nincs. Ez a monitoring/uptime feltétele — Soft Launch blocker.

## Tudásbázis referencia

- `docs/knowledge/context/CUTTING_CONTEXT.md` — hasonló modulok mintája
- `docs/knowledge/deployment/DEPLOYMENT_RUNBOOK.md` — service portok, systemd

## Feladat

KERNEL-080 mintájára (`/healthz` + `/health/ready` AllowAnonymous):

```csharp
app.MapGet("/healthz", () => Results.Ok("healthy"))
   .AllowAnonymous();

app.MapGet("/health/ready", async (ProcurementDbContext db) =>
{
    await db.Database.CanConnectAsync();
    return Results.Ok("ready");
}).AllowAnonymous();
```

## Build gate

```bash
dotnet test --no-build --verbosity minimal
# 0 fail, min 51 pass (jelenlegi baseline)
```

## DONE feltételek

- [ ] `/healthz` → 200 "healthy" auth nélkül
- [ ] `/health/ready` → 200 ha DB elérhető
- [ ] Tesztszám ≥ 51
- [ ] Commit hash
- [ ] OUTBOX DONE

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent **engedélyezett**.
