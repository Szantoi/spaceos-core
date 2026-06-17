---
id: MSG-LIBRARIAN-002
from: root
to: librarian
type: task
priority: medium
status: UNREAD
model: haiku
created: 2026-06-16
---

# Librarian — Knowledge base bővítés: migráció + INDEX frissítés

## Mi történt

A claude.ai projektből migrált dokumentumok bekerültek a knowledge base-be:

```
docs/knowledge/architecture/DESIGN_MEMORY.md   ← ÚJ — zárolt ADR döntések
docs/knowledge/INDEX.md                         ← már létezik, de frissítendő
```

Emellett a `.claude/skills/` mappában 6 új skill érhető el:
- `spaceos-arch-planner` — tervdok minőségi pipeline (v1→v4)
- `ddd-arch-planner`
- `spaceos-frontend-arch-planner`
- `spaceos-conductor`
- `spaceos-session-kickoff`
- `saas-metrics-coach`

## Feladatod

### 1. INDEX.md frissítés

Olvasd el a `docs/knowledge/INDEX.md`-t és add hozzá a DESIGN_MEMORY.md bejegyzést:

```
docs/knowledge/architecture/DESIGN_MEMORY.md — Zárolt ADR döntések katalógusa
  (claude.ai projektből migrált, 2026-06-16)
  Tartalom: ADR-010/014/018/019/020/024/039 döntések indoklással
```

### 2. Ellenőrzés

Nézd meg van-e olyan knowledge doc ami már elavult (SUPERSEDED státuszú feladatokra hivatkozik).
Ha igen, jelöld `[SUPERSEDED]` prefixszel de ne töröld — az ADR-ek megmaradnak.

### 3. Terminál memóriák következő ciklus

A következő 5 óránkénti körben (cron-librarian.sh hívja) szintetizáld a terminál memóriákat szokás szerint.

## Output

DONE outbox a szokásos formátumban.
