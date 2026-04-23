---
id: MSG-KERNEL-087-DONE
from: kernel
to: root
type: done
priority: high
status: READ
ref: MSG-KERNEL-087
created: 2026-04-17
---

## Összefoglaló

**KERNEL-087 — OpenAPI build-time JSON generálás** implementálva.

### Változások

| Fájl | Változás |
|---|---|
| `SpaceOS.Kernel.Api/Program.cs` | `app.UseSwagger()` kikerült az `IsDevelopment()` blokból → minden környezetben elérhető `/openapi/v1.json` |
| `.config/dotnet-tools.json` | `Swashbuckle.AspNetCore.Cli 6.9.0` local tool manifest |
| `docs/openapi/kernel-v1.json` | Generált OpenAPI spec (165 KB, 6074 sor) — git-be commitálva |

### Technikai megjegyzés — Swashbuckle CLI

A `dotnet swagger tofile` CLI nem kompatibilis a Minimal API (top-level statements) pattern-nel — `StartupDevelopment` class-t keres amit nem talál. Helyette a spec generálása: `dotnet run` fejlesztői módban + `curl /openapi/v1.json`. Ez megbízható, mert az app SQLite in-memory konfiggal indul.

A `.config/dotnet-tools.json` a jövőbeni CI integráció alapja — amikor a startup factory probléma megoldódik (pl. `WebApplicationFactory`-alapú approach), a CLI eszköz már elérhető.

### Commit

`df3045c` (develop)

## Tesztek

**1122 pass** (910 unit + 108 IT + 104 API), **0 fail**. Baseline teljesítve.

```
Passed! - Failed: 0, Passed: 910, Skipped: 0, Total: 910
Passed! - Failed: 0, Passed: 108, Skipped: 0, Total: 108
Passed! - Failed: 0, Passed: 104, Skipped: 4, Total: 108
```

## Security review

- [x] **Endpoint auth**: `UseSwagger` nincs `[Authorize]`-zal — szándékos, spec publikus (TypeScript kliens gen)
- [x] **SwaggerUI**: csak Development módban él, Production-ban nincs interaktív felület
- [x] **Sensitive data**: a spec tartalmaz endpointokat, de nem tartalmaz secretet/credential-t
- [x] **Input validation**: nincs új external input
- [x] **No SQL injection**: swagger middleware nem érint DB műveletet

## Kockázatok / kérdések

A `/openapi/v1.json` publikusan elérhető Production-ban is (auth nélkül). Ez szándékos — ADR-07 TypeScript kliens generáláshoz szükséges. Ha a jövőben az endpoint authot kap, a TypeScript gen workflow-t is frissíteni kell.
