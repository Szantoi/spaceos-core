---
id: MSG-KERNEL-080
from: root
to: kernel
type: task
priority: low
status: READ
created: 2026-04-15
---

# MSG-KERNEL-080 — /healthz endpoint: AllowAnonymous

## Feladat

A `/healthz` endpoint jelenleg JWT-t vár. Health check endpointok konvencionálisan publikusak — az INFRA és a load balancer is hitelesítés nélkül kell tudja lekérdezni.

Egyetlen sor változtatás: add `.AllowAnonymous()` a `/healthz` route definíciójához.

Keress rá a fájlra:
```bash
grep -rn "healthz" SpaceOS.Kernel.Api/ --include="*.cs" | head -10
```

## Ellenőrzés

```bash
dotnet test  # 1110 pass, 0 fail elvárás
curl http://localhost:5000/healthz  # 200, auth nélkül
```

## DoD

- [ ] `/healthz` visszaad 200-at JWT token nélkül
- [ ] `dotnet build` — 0 error, 0 warning
- [ ] `dotnet test` — legalább 1110 pass, 0 fail
- [ ] commit + push develop
