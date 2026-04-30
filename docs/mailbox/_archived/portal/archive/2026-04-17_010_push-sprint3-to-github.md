---
id: MSG-PORTAL-010
from: root
to: portal
type: task
priority: critical
status: READ
ref: MSG-INFRA-145-BLOCKED
created: 2026-04-17
---

# PORTAL-010 — Sprint 3 commit push GitHub-ra (INFRA blocker)

## Probléma

Az INFRA terminál nem tudja deploy-olni a Sprint 3 kódot, mert a `a57511b` commit
**nem létezik a GitHub remote-on.**

```
git cat-file -t a57511b
fatal: Not a valid object name a57511b
```

A `develop` branch remote-on nem létezik — csak `main` van, és az még `f4ce323` (Sprint 2).

## Feladat

Push-old a Sprint 3 munkát GitHub-ra:

```bash
cd /opt/spaceos/spaceos-doorstar-portal

# Ellenőrzés
git log --oneline -5
git status

# Ha develop branch-en vagy:
git push origin develop

# Ha main branch-en vagy (és oda dolgoztál):
git push origin main
```

Ha a `develop` lokálisan létezik de remote-on nem:
```bash
git push -u origin develop
```

Ha a kód `main`-re lett commitolva:
```bash
git push origin main
```

## Elvárt állapot push után

```bash
git ls-remote origin | grep -E "main|develop"
# → a57511b refs/heads/develop  (vagy main)
```

A `src/pages/` alatt legyen látható:
- `SuppliersPage.tsx`
- `InventoryPage.tsx`
- `CuttingPlansPage.tsx`
- `ProcurementPage.tsx`

## DONE feltételek

- [ ] `a57511b` elérhető GitHub-on
- [ ] `src/pages/SuppliersPage.tsx` létezik remote-on
- [ ] OUTBOX DONE üzenet a push eredménnyel (melyik branch, commit hash)

## Megjegyzés

**Kód változtatás NEM kell** — csak push. Az INFRA terminál amint látja a commitot,
automatikusan folytatja az INFRA-145 build+deploy-t.
