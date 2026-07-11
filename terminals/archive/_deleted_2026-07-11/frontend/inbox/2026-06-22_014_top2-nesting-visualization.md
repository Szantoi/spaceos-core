---
id: MSG-FRONTEND-014
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: CONSENSUS_TOP1-3_Design-Cutting-Nesting-Scheduling.md
created: 2026-06-22
content_hash: 451e3f9cfed2498481647704e5631a3664a640f895e35ebd5f6c13eb5a7b498f
---

# TOP 2: Nesting Vizualizáció SVG Canvas

## Kontextus

**Doorstar use-case:** A művezetők reggel megnézik a vágási terv anyaghatékonyságát (waste %). Jelenleg csak mock üzenet van: "Nesting API nem elérhető".

**Cél:** Valódi nesting vizualizáció SVG canvas-en, waste % badge, strategy pill.

## Feladat

Implementálj egy SVG-alapú nesting viewer komponenst, amely megjeleníti a vágási terv elrendezését és anyaghulladék adatokat.

### Implementációs scope

#### 1. ProductionPage Nesting Viewer Integráció

**Fájl:** `src/pages/ProductionPage.tsx`

**Változtatások:**
- `useApi()` hook: `GET ${API_BASE.cutting}/api/cutting/sheets/${selectedPlan}/nesting`
- Ha API elérhető → `<NestingViewer data={nestingData} />` renderelés
- Ha API hiba → fallback: jelenlegi mock üzenet megmarad

#### 2. NestingViewer Komponens (ÚJ)

**Fájl:** `src/components/nesting/NestingViewer.tsx`

**Props:**
```typescript
interface NestingViewerProps {
  data: {
    sheets: Array<{
      id: string;
      width: number;    // mm
      height: number;   // mm
      placedParts: Array<{
        x: number;
        y: number;
        width: number;
        height: number;
        materialType: string;
        partId: string;
      }>;
      wastePercentage: number;
    }>;
    strategy: "Guillotine" | "FFDH";
  };
}
```

**SVG Canvas implementáció:**
- Viewport: auto-scale a legnagyobb sheet méretére (pl. 2800×2070mm → 700×517px, 4:1 scale)
- Background: light gray rectangle (panel)
- Placed parts: colored rectangles (CATALOG_LOOKUP szerinti színkódolás)
- Hover tooltip: part ID, dimensions, material type

**Stats Badge (top-right corner):**
- Waste %: `{wastePercentage.toFixed(1)}%`
  - Red badge ha >15%
  - Yellow badge ha 10-15%
  - Green badge ha <10%
- Strategy pill: `{strategy}`
- Sheets count: `{sheets.length} lap`

**Per-sheet Navigation (ha `sheets.length > 1`):**
- Previous/Next buttons
- Sheet indicator: "2 / 3"

**Material Type Filter Dropdown (opcionális):**
- CATALOG_LOOKUP unique values
- Click → filter parts by material type

#### 3. CATALOG_LOOKUP Integráció

**Ellenőrizni:**
- `/opt/spaceos/frontend/joinerytech-portal/src/mocks/data.ts`
- Ha nincs `CATALOG_LOOKUP`, létrehozni mock színekkel:
  ```typescript
  export const CATALOG_LOOKUP = {
    "Bükk": { color: "amber" },
    "MDF": { color: "stone" },
    "Tölgy": { color: "brown" },
    // ...
  };
  ```

### DoD

- [ ] ProductionPage nesting viewer megjeleníti a `GET /cutting/api/cutting/sheets/{id}/nesting` API adatait
- [ ] SVG canvas scale-zett panel + placed parts színkódolt rectangles (CATALOG_LOOKUP szerint)
- [ ] Stats badge: Waste % (color-coded), Strategy, Sheets count
- [ ] Per-sheet navigation (ha >1 sheet)
- [ ] +13 FE teszt pass:
  - `NestingViewer.test.tsx`: 8 teszt (SVG rendering, scale calculation, color mapping, sheet navigation, stats badge)
  - `ProductionPage.test.tsx`: 5 teszt (API hívás, fallback logic)
- [ ] `pnpm test` pass (742+ tests)
- [ ] `pnpm build` 0 error

### Backend API ellenőrzés

**Endpoint:** ✅ READY — `GET /cutting/api/cutting/sheets/{id}/nesting`
**Status:** 931/931 teszt, deployed

**Response:**
```json
{
  "sheets": [
    {
      "id": "uuid",
      "width": 2800,
      "height": 2070,
      "placedParts": [
        {
          "x": 0,
          "y": 0,
          "width": 600,
          "height": 400,
          "materialType": "Bükk",
          "partId": "part-123"
        }
      ],
      "wastePercentage": 12.3
    }
  ],
  "strategy": "Guillotine"
}
```

### Implementációs irányelvek

**SVG Best Practices:**
- `viewBox` dinamikus számítás: `0 0 ${scaledWidth} ${scaledHeight}`
- Scale factor: `Math.min(700 / maxWidth, 517 / maxHeight)`
- Hover interakció: `<title>` SVG elem vagy custom tooltip component
- Color mapping: Tailwind utility classes (amber-500, stone-500, brown-500)

**Kockázatok:**
- ⚠️ API response schema változás (pl. `placedParts` helyett `placements`) → strict TypeScript interface kell
- ⚠️ SVG performance ha >100 part egy sheet-en → virtualiz áció NEM szükséges Doorstar skálán (max 30-40 part/sheet)

### Kapcsolódó fájlok

- `frontend/joinerytech-portal/src/pages/ProductionPage.tsx`
- `frontend/joinerytech-portal/src/components/nesting/NestingViewer.tsx` (ÚJ)
- `frontend/joinerytech-portal/src/mocks/data.ts` (CATALOG_LOOKUP)
- `frontend/joinerytech-portal/src/hooks/useApi.ts`

### Becsült idő

**3-4 nap** (SVG rendering + scale logic + stats + 13 teszt)

---

**Priority:** HIGH — Doorstar workflow törött pont #2, anyaghatékonyság láthatóvá tétele
**Ref:** `/opt/spaceos/docs/tasks/archive/CONSENSUS_TOP1-3_Design-Cutting-Nesting-Scheduling.md`
