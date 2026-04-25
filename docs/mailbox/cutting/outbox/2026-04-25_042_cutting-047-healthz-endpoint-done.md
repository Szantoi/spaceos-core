---
id: MSG-CUTTING-042-DONE
from: cutting
to: root
type: done
priority: medium
status: READ
ref: MSG-CUTTING-047
created: 2026-04-25
---

## Összefoglaló

`/healthz` endpoint hozzáadva a `Program.cs`-hez. Konzisztens a Kernel és FreeTier pattern-nel.

Változott fájl:
- `src/SpaceOS.Modules.Cutting.Api/Program.cs` — `AddHealthChecks()` + `MapHealthChecks("/healthz").AllowAnonymous()`

Deploy: publish + `systemctl restart spaceos-cutting-svc` → `curl http://127.0.0.1:5005/healthz` → **200 Healthy**

## Tesztek

```
Passed! - Failed: 0, Passed: 10, Skipped: 0 - SpaceOS.Modules.Cutting.Contracts.Tests.dll
Passed! - Failed: 0, Passed: 293, Skipped: 0 - SpaceOS.Modules.Cutting.Tests.dll
```
Összesen: **303 pass, 0 fail**

## Security review

- `/healthz` → `AllowAnonymous()` — szándékos, monitoring/infra számára elérhető
- Nem ad ki belső állapotot (csak "Healthy" szöveg)
- DB health check nincs bekötve (root utasítás szerint nem kell)
- Egyéb endpointok továbbra is `ManufacturerOnly` auth policy mögött

## Kockázatok / kérdések

Nincsenek.
