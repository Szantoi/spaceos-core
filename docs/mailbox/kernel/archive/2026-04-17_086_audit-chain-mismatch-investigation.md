---
id: MSG-KERNEL-086
from: root
to: kernel
type: task
priority: critical
status: READ
ref: SPRINT4
created: 2026-04-17
---

# KERNEL-070 — Audit chain hash mismatch vizsgálat + fix

## Kontextus

A `verify-chain` végpont `isValid=false`-t ad concurrent writes után.
Az advisory lock race condition korábban azonosított (T-01 tech debt a Codebase_Status-ban),
de fix még nem deployed. Ez az **escrow/WORM előfeltétel** — Doorstar Soft Launch P0.

## Tudásbázis referencia

Olvasd el session indításakor:
- `docs/knowledge/context/KERNEL_CONTEXT.md` — terminál kontextus
- `docs/knowledge/patterns/DATABASE_PATTERNS.md` — advisory lock MD5 minta
- `docs/knowledge/security/SECURITY_PATTERNS.md` — audit chain hash szerkezet

## Feladat

1. **Diagnózis**: reprodukáld a hash mismatch-et concurrent writes-szal (teszt)
2. **Root cause**: advisory lock race condition — mi pontosan a sorrend hiba?
3. **Fix**: serializable audit event insert VAGY advisory lock VAGY DB-szintű SEQUENCE
4. **Tesztek**: concurrent write teszt ami zöld a fix után
5. **verify-chain**: `isValid=true` minden chain-re a fix után

## Build gate

```bash
dotnet test --no-build --verbosity minimal
# 0 fail, min 1121 pass (jelenlegi baseline)
```

## DONE feltételek

- [ ] Root cause dokumentálva
- [ ] Fix implementálva + teszt
- [ ] `verify-chain isValid=true` concurrent writes után
- [ ] Tesztszám: ≥ 1121 (jelenlegi baseline)
- [ ] Commit hash
- [ ] OUTBOX DONE: root cause leírás, fix összefoglaló, tesztszám

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent **engedélyezett**.
