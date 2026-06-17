---
id: MSG-FE-062
from: root
to: fe
type: task
priority: high
status: READ
model: sonnet
ref: MSG-ROOT-001
created: 2026-06-17
---

# FE — TOP 2: Nesting Visualization (APPROVED)

## Összefoglaló

TOP 2 **APPROVED** — azonnali indítható TOP 1 után (vagy párhuzamosan).

**Scope:** ProductionPage nesting viewer komponens + `GET /cutting/api/cutting/sheets/{id}/nesting` API integráció + SVG canvas + waste % badge

**Becs. munka:** 3-4 nap FE (0 backend)

---

## Implementation spec (consensus tervből)

### ProductionPage nesting viewer

**Jelenlegi:** "Nesting API nem elérhető" mock üzenet
**Cél:** SVG canvas + stats badge

### Új komponens: NestingViewer.tsx

**Props:** `{ data: NestingResultDto }`

1. **SVG canvas:**
   - Viewport: auto-scale a legnagyobb sheet méretére (pl. 2800×2070mm → 700×517px, 4:1 scale)
   - Background: light gray rectangle (panel)
   - Placed parts: colored rectangles (CATALOG_LOOKUP szerinti szín kódolás)
   - Hover tooltip: part ID, dimensions, material type

2. **Stats badge (top-right corner):**
   - Waste %: `{wastePercentage.toFixed(1)}%` (red if >15%, yellow if 10-15%, green if <10%)
   - Strategy: `{strategy}` pill (pl. "Guillotine", "FFDH")
   - Sheets count: `{sheets.length} lap`

3. **Per-sheet toggle (ha `sheets.length > 1`):**
   - Previous/next buttons
   - Sheet indicator (pl. "2 / 3")

4. **Material type filter dropdown (opcionális):**
   - CATALOG_LOOKUP unique values

### API integráció

- `GET ${API_BASE.cutting}/api/cutting/sheets/${selectedPlan}/nesting`
- Response schema: `{ sheets: [{ id, width, height, placedParts: [...], wastePercentage }], strategy: "Guillotine" | "FFDH" }`
- Ha API hiba → fallback: jelenlegi mock üzenet megmarad

### Teszt cél

- +8 NestingViewer.test.tsx: SVG rendering, scale calculation, color mapping, sheet navigation, stats badge
- +5 ProductionPage.test.tsx: API hívás, fallback logic
- Teljes FE teszt coverage: ~771 → 784 teszt

### DoD (Definition of Done)

- [ ] ProductionPage nesting viewer megjeleníti a `GET /cutting/api/cutting/sheets/{id}/nesting` API adatait
- [ ] SVG canvas scale-zett panel + placed parts szín-kódolt rectangles (CATALOG_LOOKUP szerint)
- [ ] Stats badge: Waste % (color-coded), Strategy, Sheets count
- [ ] Per-sheet navigation (ha >1 sheet)
- [ ] +13 FE teszt pass (8 viewer + 5 page)
- [ ] 0 pnpm build error

---

## Kockázatok

1. **API response schema eltérés** — `placedParts` helyett `placements` stb.
   - **Mitigation:** strict TypeScript interface + Zod runtime validation
   - **Fallback:** error toast, fallback mock üzenet

2. **SVG performance** — >100 part egy sheet-en
   - **Impact:** Doorstar skálán nem problém (max 30-40 part/sheet)
   - **Mitigation:** virtualizáció NEM szükséges

3. **CATALOG_LOOKUP hiánya** — materialType enum nem egyezik
   - **Mitigation:** FE-ben mock CATALOG_LOOKUP kitöltés (amber, stone, brown stb.)

---

## Relationship (TOP 1-vel)

TOP 2 **nem függ** TOP 1-től technikai szinten, de **logikailag kapcsolódik:**
- TOP 1 létrehozza az új plan-okat
- TOP 2 azokat vizualizálja (nesting data-val)

**Ajánlott sorrend:** TOP 1 → TOP 2 (vagy párhuzamos FE development)

---

## Siguiente lépés

1. FE terminál: TOP 1 implementáció (2-3 nap)
2. FE terminál: TOP 2 implementáció (3-4 nap párhuzamosan vagy után)
3. Deploy + Doorstar smoke test

🚀 Indítás: **AZONNAL** (TOP 1 után)
