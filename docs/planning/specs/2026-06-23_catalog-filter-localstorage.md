---
id: SPEC-2026-06-23-FILTER
title: "Dinamikus Katalógus Filter-panel szinkronizálása localStorage-ba"
type: spec
priority: medium
status: IMPLEMENTÁCIÓRA KÉSZ
source_idea: IDEA-20260623-001
assignee: frontend
model: sonnet
created: 2026-06-23
---

# Dinamikus Katalógus Filter-panel szinkronizálása localStorage-ba

## 1. Cél és Scope

**Probléma:** A `catalog-world-view.jsx` (`WorldCatalog` komponens) szűrői (kategória, pinned filter, keresés, nézet mód) nem perzisztálódnak. Oldal frissítéskor minden beállítás elvész.

**Megoldás:** A kiválasztott szűrőket localStorage-ba menteni, és komponens mount-kor visszatölteni.

**Scope:**
- `catalog-world-view.jsx` — `WorldCatalog` komponens módosítása
- Világ-specifikus szűrő perzisztálás (`design`, `interior`, `procurement`, stb.)
- Nincs app-store módosítás (localStorage elég)

**Perzisztálandó állapotok:**

| State | Típus | Alapértelmezett |
|-------|-------|-----------------|
| `q` | string | `""` |
| `filterCat` | string | `"all"` |
| `activePinned` | string \| null | `null` |
| `viewMode` | `"grid"` \| `"list"` | `"grid"` |

## 2. Architektúra

### Érintett komponensek

```
/docs/joinerytech/catalog-world-view.jsx
  └── WorldCatalog({ worldId, ... }) komponens (308-620. sor)
        └── useState hook-ok (328-332. sor)
```

### Jelenlegi state deklarációk

```javascript
// 328-332. sor
const [q, setQ] = useWC("");
const [filterCat, setFilterCat] = useWC("all");
const [activePinned, setActivePinned] = useWC(null);
const [viewMode, setViewMode] = useWC("grid");
const [showAddPin, setShowAddPin] = useWC(false);
```

### localStorage kulcs séma

```javascript
const LS_KEY = `jt_catalog_filters_${worldId}`;

// Példa: "jt_catalog_filters_design"
// Tárolt érték:
{
  q: "tölgy",
  filterCat: "Lapanyag",
  activePinned: "lapanyag",
  viewMode: "list"
}
```

### Javasolt implementáció

```javascript
// Helper függvények (fájl elején, WORLD_CATALOG_CONFIG után)
const CATALOG_LS_PREFIX = "jt_catalog_filters_";

function loadCatalogFilters(worldId) {
  try {
    const raw = localStorage.getItem(CATALOG_LS_PREFIX + worldId);
    if (!raw) return null;
    return JSON.parse(raw);
  } catch (e) {
    return null;
  }
}

function saveCatalogFilters(worldId, filters) {
  try {
    localStorage.setItem(CATALOG_LS_PREFIX + worldId, JSON.stringify(filters));
  } catch (e) {
    // localStorage full vagy disabled — silent fail
  }
}

// WorldCatalog komponensben (308. sor körül)
function WorldCatalog({ worldId, onItemClick, extraHeader }) {
  // ... existing hooks ...

  // --- MÓDOSÍTOTT state inicializálás ---
  const savedFilters = useMemoWC(() => loadCatalogFilters(worldId), [worldId]);

  const [q, setQ] = useWC(savedFilters?.q ?? "");
  const [filterCat, setFilterCat] = useWC(savedFilters?.filterCat ?? "all");
  const [activePinned, setActivePinned] = useWC(savedFilters?.activePinned ?? null);
  const [viewMode, setViewMode] = useWC(savedFilters?.viewMode ?? "grid");
  const [showAddPin, setShowAddPin] = useWC(false); // nem perzisztálunk

  // --- MENTÉS minden változáskor ---
  React.useEffect(() => {
    saveCatalogFilters(worldId, { q, filterCat, activePinned, viewMode });
  }, [worldId, q, filterCat, activePinned, viewMode]);

  // ... rest of the component ...
}
```

## 3. State Management

### localStorage séma

```
localStorage
├── jt_catalog_filters_design       → { q, filterCat, activePinned, viewMode }
├── jt_catalog_filters_interior     → { q, filterCat, activePinned, viewMode }
├── jt_catalog_filters_procurement  → { q, filterCat, activePinned, viewMode }
├── jt_catalog_filters_production   → { q, filterCat, activePinned, viewMode }
├── jt_catalog_filters_trade        → { q, filterCat, activePinned, viewMode }
├── jt_catalog_filters_warehouse    → { q, filterCat, activePinned, viewMode }
└── jt_catalog_filters_sales        → { q, filterCat, activePinned, viewMode }
```

### Világ-váltás kezelése

Amikor a felhasználó másik világba navigál:
1. Az új `worldId` változik
2. `useMemo` újraszámolja `savedFilters`-t
3. `useState` alapértelmezettek az új világ mentett értékei lesznek

**Megjegyzés:** A `useState` inicializáló függvény csak mount-kor fut le. Ha a `worldId` változik anélkül hogy a komponens unmount-olna, a state nem frissül automatikusan. Ez probléma lehet ha a `WorldCatalog` nem renderelődik újra világváltáskor.

**Megoldás A (ajánlott):** A világváltás unmount-olja a komponenst (`key={worldId}` a szülőben).

**Megoldás B:** useEffect a worldId változás figyelésére:

```javascript
React.useEffect(() => {
  const saved = loadCatalogFilters(worldId);
  setQ(saved?.q ?? "");
  setFilterCat(saved?.filterCat ?? "all");
  setActivePinned(saved?.activePinned ?? null);
  setViewMode(saved?.viewMode ?? "grid");
}, [worldId]);
```

## 4. UI/UX Terv

### Nincs UI változás

A funkcionalitás transzparens a felhasználó számára:
- Szűrő beállítások automatikusan mentődnek
- Oldal frissítés után visszaállnak
- Nincs "Mentés" gomb vagy indikátor

### Edge case-ek

1. **Érvénytelen mentett kategória:** Ha a mentett `filterCat` már nem létezik (pl. törölték a kategóriát), fallback `"all"`-ra.

```javascript
// Validáció a cats array-jel
const validCat = cats.includes(savedFilters?.filterCat) ? savedFilters.filterCat : "all";
const [filterCat, setFilterCat] = useWC(validCat);
```

2. **Érvénytelen mentett pin:** Ha a mentett `activePinned` nem létezik (törölt custom pin):

```javascript
const validPin = [...(cfg.pinnedFilters || []).map(f => f.key), ...customPins.map(p => p.id)]
  .includes(savedFilters?.activePinned) ? savedFilters.activePinned : null;
const [activePinned, setActivePinned] = useWC(validPin);
```

## 5. Definition of Done (DoD)

- [ ] `loadCatalogFilters()` és `saveCatalogFilters()` helper függvények létrehozva
- [ ] `WorldCatalog` state inicializálás módosítva localStorage-ból
- [ ] `useEffect` hozzáadva a mentéshez
- [ ] Világ-váltás helyesen kezeli a különböző mentett állapotokat
- [ ] Érvénytelen mentett értékek graceful fallback-kel kezelve
- [ ] localStorage kulcsok konzisztensek (`jt_catalog_filters_{worldId}`)
- [ ] Oldal frissítés után a szűrők megmaradnak
- [ ] Nincs console error
- [ ] Meglévő funkcionalitás (szűrés, keresés, nézet váltás) változatlanul működik

## 6. Becsült idő

**1-2 óra**

Részletezés:
- Helper függvények: 15 perc
- State inicializálás módosítás: 20 perc
- useEffect mentés: 15 perc
- Világváltás kezelés: 20 perc
- Validáció (érvénytelen értékek): 15 perc
- Tesztelés: 20 perc

## 7. Terminál hozzárendelés

**frontend** — React state management + localStorage integráció

## 8. Kockázatok és megjegyzések

1. **localStorage quota:** A szűrő objektumok kicsik (~100 byte/világ). 7 világ = ~700 byte. Nem probléma.

2. **Private browsing:** Egyes böngészők private módban nem engedik a localStorage-ot. A `try/catch` wrapper kezeli ezt — silent fail, nincs perzisztálás.

3. **JSON parse hiba:** Ha a mentett érték korrupt, `loadCatalogFilters()` `null`-t ad vissza és az alapértelmezettek használódnak.

4. **Keresési lekérdezés (`q`) perzisztálása:** Opcionális — ha zavaró hogy a keresőmező nem üres oldal frissítés után, a `q` kihagyható a mentésből. Döntés a frontend-re.

5. **Custom pin validáció:** A `customPins` a `sim.worldCatalogPins` store-ból jön. Ha a pin törlődik de a localStorage-ban még szerepel, a validáció `null`-ra állítja az `activePinned`-et.

---

*Generálva: 2026-06-23 | Forrás idea: IDEA-20260623-001 | Architect terminál*
