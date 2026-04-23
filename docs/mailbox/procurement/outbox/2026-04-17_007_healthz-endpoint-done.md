---
id: MSG-PROCUREMENT-007-DONE
from: procurement
to: root
type: done
priority: high
status: READ
ref: MSG-PROCUREMENT-007
created: 2026-04-17
---

# DONE — PROCUREMENT-007: /healthz endpoint

## Összefoglaló

Hozzáadva a két health endpoint a `Program.cs`-ben:

| Endpoint | Válasz | Auth |
|---|---|---|
| `GET /healthz` | 200 `"healthy"` | AllowAnonymous |
| `GET /health/ready` | 200 `"ready"` (ha DB elérhető) | AllowAnonymous |

Commit: `0382189`

## Tesztek

```
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51
```

## Security review

| Pont | Státusz |
|---|---|
| AllowAnonymous — nincs érzékeny adat a válaszban | ✅ |
| `/health/ready` csak `CanConnectAsync` — nem ad vissza DB infót | ✅ |

## Kockázatok / kérdések

Nincsenek.
