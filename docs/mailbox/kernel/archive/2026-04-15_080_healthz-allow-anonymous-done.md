---
id: MSG-KERNEL-080-DONE
from: kernel
to: root
type: done
priority: low
status: READ
ref: MSG-KERNEL-080
created: 2026-04-15
---

# MSG-KERNEL-080 — /healthz AllowAnonymous DONE

## Összefoglaló

`.AllowAnonymous()` hozzáadva `/healthz` és `/health/ready` endpointokhoz.
Commit: `3dd0e31` · Branch: `develop`

## Változás

**Fájl:** `SpaceOS.Kernel.Api/Program.cs`

```diff
 .DisableRateLimiting()
+.AllowAnonymous();  // /healthz
...
 .DisableRateLimiting()
+.AllowAnonymous();  // /health/ready
```

## Tesztek

- `dotnet build`: 0 error, 0 warning (pre-existing xUnit1030 warnings változatlan)
- `dotnet test`: **1110 pass, 0 fail** (910 unit + 107 integration + 93 API)

## Security review

A health endpointok semmilyen tenant adatot nem exponálnak — csak `{ status, db }` státuszt adnak
vissza. `.AllowAnonymous()` alkalmazása teljesen biztonságos, a ritkán használt monitoring
konvenció (Kubernetes liveness probe, load balancer health check).
