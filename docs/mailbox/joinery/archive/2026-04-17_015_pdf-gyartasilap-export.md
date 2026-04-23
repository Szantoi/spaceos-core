---
id: MSG-JOINERY-015
from: root
to: joinery
type: task
priority: critical
status: READ
ref: SPRINT5
created: 2026-04-17
---

# JOINERY-015 — PDF Gyártásilap export (JOINERY-V2 fázis 1)

## Kontextus

A Doorstar Soft Launch GO után a JOINERY-V2 backlog item feloldódott.
A Doorstar számára a legértékesebb termelési dokumentum a gyártásilap PDF —
ezt nyomtatják ki és viszik a műhelybe. Ez az első éles üzleti funkció
amit a valódi felhasználók kézbe vesznek.

## Tudásbázis referencia

- `docs/knowledge/context/JOINERY_CONTEXT.md` — terminál kontextus
- `docs/knowledge/patterns/DATABASE_PATTERNS.md` — RLS, Testcontainers
- `docs/knowledge/architecture/API_CONTRACT_CATALOGUE.md` — meglévő API surface

## Feladat

### Új endpoint

```
GET /api/orders/{id}/manufacturing-sheet
→ 200 application/pdf
→ 401 ha nincs auth
→ 403 ha más tenant
→ 404 ha nem létezik az order
```

### PDF tartalma

**Fejléc:**
- Doorstar Kft. (fix, vagy tenant névből)
- Rendelésszám, dátum, szállítási határidő
- Ügyfél neve, cím (ProjectInfo VO-ból)

**Tételek táblázat (DoorItems):**
- Sorszám, ajtótípus, szélesség × magasság, anyag, felület, megjegyzés

**Összesítő:**
- Összes darab, anyagnorma összesítés (ha elérhető a CalculateDoorOrder-ből)

### PDF generálás

Ajánlott: `QuestPDF` (.NET natív, MIT license) — vagy bármilyen .NET 8 kompatibilis lib.
Ha más lib-et választasz, indokold az outbox-ban.

### Tesztek

- Unit: PDF tartalom tesztelése (headless, byte[] nem null, tartalmazza az orderszámot)
- API: 200 `application/pdf`, 401 auth nélkül, 403 más tenant
- Tesztszám: ≥ 230 (jelenlegi baseline: 219)

## Build gate

```bash
dotnet test --no-build --verbosity minimal
# 0 fail, min 230 pass
```

## DONE feltételek

- [ ] `GET /api/orders/{id}/manufacturing-sheet` → 200 `application/pdf`
- [ ] PDF fejléc + tételek + összesítő
- [ ] 401 / 403 / 404 kezelés
- [ ] Tesztszám ≥ 230
- [ ] Commit hash
- [ ] OUTBOX DONE: lib választás indoklás + tesztszám

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent **engedélyezett**.
