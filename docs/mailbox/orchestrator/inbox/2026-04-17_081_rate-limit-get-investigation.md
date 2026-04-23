---
id: MSG-ORCH-081
from: root
to: orch
type: task
priority: high
status: READ
ref: SPRINT4
created: 2026-04-17
---

# ORCH-081 — Rate limit GET route vizsgálat

## Kontextus

E2E-047 (Sprint 2 full rerun) során a `27-rate-limit` teszt "GET requests are not
rate limited" lépése elbukott. Feltételezés: az ORCH-077..080 változtatások után
a GET route-ok is rate limitáltak lettek, ami nem szándékos.

## Tudásbázis referencia

- `docs/knowledge/context/ORCH_CONTEXT.md` — terminál kontextus
- `docs/knowledge/security/SECURITY_PATTERNS.md` — requireAuth > rateLimiter sorrend

## Feladat

1. **Diagnózis**: futtasd a `27-rate-limit` tesztet izoláltan
   ```bash
   npx vitest run tests/27-rate-limit.chain.test.ts
   ```
2. **Root cause**: a rate limiter middleware kiterjed GET route-okra is?
   - Ha igen: ez szándékos (prod security) → teszt frissítés kell
   - Ha nem szándékos: middleware order vagy config fix
3. **Fix**: konfiguráció VAGY teszt javítás
4. **Rerun**: teljes teszt suite zöld

## Build gate

```bash
npm run test -- --run
# 0 fail, min 218 pass (jelenlegi baseline)
```

## DONE feltételek

- [ ] 27-rate-limit teszt diagnózis (szándékos vs bug)
- [ ] Fix (middleware config VAGY teszt frissítés)
- [ ] Tesztszám ≥ 218
- [ ] Commit hash
- [ ] OUTBOX DONE: root cause + mi volt a döntés

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent **engedélyezett**.
