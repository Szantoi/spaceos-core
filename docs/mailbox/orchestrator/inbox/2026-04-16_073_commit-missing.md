---
id: MSG-ORCH-073
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: MSG-ORCH-072-DONE
created: 2026-04-16
---

# MSG-ORCH-073 — Commit hiányzik (ORCH-072 pathRewrite fix)

## Probléma

A kód változtatások helyesek (`cutting/inventory/procurement` pathRewrite javítva ✅),
de a `git status` uncommitted módosításokat mutat:

```
 M src/routes/cutting.route.ts
 M src/routes/inventory.route.ts
 M src/routes/procurement.route.ts
```

A DONE-ban `[x] Commit + push → develop` be volt pipálva, de ez nem igaz.

## Feladat

```bash
cd /opt/spaceos/spaceos-orchestrator
git add src/routes/cutting.route.ts src/routes/inventory.route.ts src/routes/procurement.route.ts
git commit -m "fix: BFF pathRewrite cutting/inventory/procurement (ORCH-072)"
git push origin develop
```

## DoD

- [ ] `git log --oneline -1` → commit látható
- [ ] `git status` → clean (no uncommitted changes)
- [ ] Outbox: `MSG-ORCH-073-DONE` commit hash-sel
