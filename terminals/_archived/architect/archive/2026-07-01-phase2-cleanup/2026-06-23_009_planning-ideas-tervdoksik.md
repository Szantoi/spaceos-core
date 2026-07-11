---
id: MSG-ARCHITECT-009
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
created: 2026-06-23
content_hash: 580741e17ef718b4a74da74672d5e3da9a73505ec17be8424431f37d6f494295
---

# Tervdoksik készítése Planning Ideas-ből

## Kontextus

A planning queue üres, de **6 új ötlet** várja feldolgozásra a `docs/planning/ideas/` mappában.
Ezek a **JoineryTech prototípusból** (datahaven-web UI) származnak.

## Feladat

Válassz ki **2-3 legértékesebb ötletet** és készíts róluk **részletes tervdoksikat** a következő formátumban:

```
docs/planning/specs/YYYY-MM-DD_<slug>.md
```

### Elérhető ötletek

**Részletes (UI feature-ök):**
1. **Katalógus Filter localStorage** (`2026-06-23_001`) - State management, ~1-2 óra
   - Filter panel szinkronizálás localStorage-ba
   - catalog-manager.jsx + app-store.jsx érintett

2. **Assembly Drag-and-Drop** (`2026-06-23_002`) - UI komponens, ~2-3 óra
   - Szerelési műveletek átrendezése drag-and-drop-pal
   - dnd-kit vagy HTML5 Drag API
   - assembly.jsx + app-store.jsx érintett

3. **Katalógus Lazy-load** (`2026-06-23_003`) - UI optimalizáció, ~1 óra
   - Képek lazy loading + skeleton
   - "Nincs kép" fallback badge
   - catalog-world-view.jsx érintett

**Egyszerű (készlet badge-ek):**
4. **Zöld badge** (`2026-06-23_004`) - raktáron van elég
5. **Sárga badge** (`2026-06-23_005`) - részben elérhető
6. **Piros badge** (`2026-06-23_006`) - nincs raktáron

## Prioritási javaslat

Javasolt sorrend **üzleti érték + komplexitás** alapján:

1. **Katalógus Lazy-load** (ötlet #3) - GYORS WIN
   - Kis méret (~1 óra)
   - Azonnali UX javulás (mobil adatfogyasztás ↓)
   - Nincs state management komplexitás

2. **Assembly Drag-and-Drop** (ötlet #2) - NAGY ÉRTÉK
   - Használhatósági áttörés a dolgozóknak
   - 2-3 óra, külső library kell
   - Jól definiált scope

3. **Katalógus Filter localStorage** (ötlet #1) - KÖZEPES ÉRTÉK
   - UX javulás (keresési állapot megőrzése)
   - State management minta alkalmazása
   - 1-2 óra

A badge-ek (4-6) később csatolhatók bármely katalógus/készlet feature-höz.

## Tervdoksi követelmények

Minden tervdoksiban legyen:

1. **Cél és scope** - Mit implementálunk pontosan?
2. **Architektúra** - Melyik komponensek érintettek?
3. **State management** - localStorage / app-store integráció
4. **UI/UX terv** - Tailwind stílusok, interakciók
5. **DoD (Definition of Done)** - Mikor tekintjük késznek?
6. **Becsült idő** - 1-3 óra intervallum
7. **Terminál hozzárendelés** - frontend (mindhárom UI feature)

## Kimenet

- 2-3 tervdoksi: `docs/planning/specs/2026-06-23_<slug>.md`
- DONE outbox üzenet a kész tervdoksikról
- Az ötleteket NE töröld a `docs/planning/ideas/`-ból (pipeline majd archiválja)

## DoD

- [ ] 2-3 tervdoksi elkészült (`docs/planning/specs/`)
- [ ] Minden tervdoksi tartalmaz architektúra leírást
- [ ] Minden tervdoksi tartalmaz DoD szekciót
- [ ] DONE outbox üzenet a conductor-nak

---

**Kezdd a legegyszerűbb ötlettel** (Katalógus Lazy-load), hogy gyorsan legyen eredmény!
