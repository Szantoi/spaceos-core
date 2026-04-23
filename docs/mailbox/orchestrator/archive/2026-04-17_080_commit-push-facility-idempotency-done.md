---
id: MSG-ORCH-080-DONE
from: orchestrator
to: root
type: done
priority: high
status: READ
ref: MSG-ORCH-080
created: 2026-04-17
---

# ORCH-080 DONE — commit+push + facility idempotencia fix

## Összefoglaló

| Fájl | Változás |
|---|---|
| `src/routes/test.route.ts` | Facility name: `'Doorstar Gyártó'` → `` `Doorstar Gyártó ${Date.now()}` `` (mindkét profil) |
| `src/routes/test.route.test.ts` | Nem változott (facility nevet a tesztek nem assertálják) |

**ORCH-079 + ORCH-080 egyetlen commitban mentek** (mindkét változás ugyanabban a fájlban volt,
nem lehetett tisztán szeparálni):

```
commit bb795ab
fix(seed): add DoorItem before submit + unique facility name (ORCH-079/080)
```

**Git állapot:**
```
develop branch: bb795ab (pushed to origin)
git log --oneline -3:
  bb795ab fix(seed): add DoorItem before submit + unique facility name (ORCH-079/080)
  4e8926d fix(seed): facility-first FlowEpic + supplier seeding (ORCH-078)
  4497f45 feat(test-seed): doorstar-cutting-ready-v1 seed profile (ORCH-077)
```

## Tesztek

- **218/218 pass** (29 test file)
- 0 TypeScript error
- Facility name teszt: a tesztek csak hívásszámot és response shape-t assertálnak, facility nevet nem — nincs törés

## Security review

- `Date.now()` server-oldalon generált, nem user input — injection kockázat nulla
- Seed endpointok `testGuard` mögött maradnak (flag + secret + allowlist)
- Token nem logolva
- OWASP: nincs új felület

## Kockázatok / kérdések

Nincs. A seed pipeline idempotens — ugyanazt a profilt egymás után futtatva többször is helyes eredményt ad.

**Következő lépés:** INFRA-143 — ORCH `bb795ab` redeploy (pm2 restart) + idempotencia smoke-teszt
