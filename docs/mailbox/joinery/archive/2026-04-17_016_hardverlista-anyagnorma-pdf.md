---
id: MSG-JOINERY-016
from: root
to: joinery
type: task
priority: high
status: READ
ref: SPRINT6
created: 2026-04-17
---

# JOINERY-016 — JOINERY-V2 fázis 2: Hardverlista + Anyagnorma PDF

## Kontextus

Sprint 5 (JOINERY-015) bevezette a QuestPDF-alapú gyártásilap PDF-et.
Sprint 6-ban a másik két meglévő JSON endpoint PDF változatát kell elkészíteni.
A `GetHardwareListQueryHandler` és `GetMaterialRequirementsQueryHandler` már létezik —
csak PDF outputot kell hozzájuk adni.

## Tudásbázis referencia

- `docs/knowledge/context/JOINERY_CONTEXT.md` — terminál kontextus
- JOINERY-015 implementáció mintaként (`manufacturing-sheet` endpoint + QuestPDF pattern)

## Feladat

### 1. Hardverlista PDF

```
GET /api/orders/{id}/hardware-list-pdf
→ 200 application/pdf
→ 401 auth nélkül
→ 403 más tenant
→ 404 nem létező order
```

**PDF tartalom:**
- Fejléc: rendelésszám, kelt, ügyfélnév (ProjectInfo-ból)
- Tételek táblázat: Ssz., Megnevezés, Egység, Mennyiség, Megjegyzés
- Összesítő: tételszám

### 2. Anyagnorma PDF

```
GET /api/orders/{id}/material-req-pdf
→ 200 application/pdf
→ 401 / 403 / 404
```

**PDF tartalom:**
- Fejléc: rendelésszám, kelt, ügyfélnév
- Anyagcsoportok: lapanyag, élzáró, felületkezelő, egyéb
- Összesítő: összköltség-becslés ha van

### Közös szabályok (mint manufacturing-sheet)

- `[Authorize("ManufacturerOnly")]`
- `Cache-Control: private, no-store`
- `X-Content-Type-Options: nosniff`
- QuestPDF — már approved dependency

## Build gate

```bash
dotnet test --no-build --verbosity minimal
# 0 fail, min 245 pass (jelenlegi: 231)
```

## DONE feltételek

- [ ] `GET /api/orders/{id}/hardware-list-pdf` → 200 `application/pdf`
- [ ] `GET /api/orders/{id}/material-req-pdf` → 200 `application/pdf`
- [ ] 401 / 403 / 404 mindkét endpointon
- [ ] Cache-Control: no-store mindkét endpointon
- [ ] Tesztszám ≥ 245
- [ ] Commit hash
- [ ] OUTBOX DONE

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent **engedélyezett**.
