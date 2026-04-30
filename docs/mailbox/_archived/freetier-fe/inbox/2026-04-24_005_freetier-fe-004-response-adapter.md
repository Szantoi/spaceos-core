---
id: MSG-FREETIER-FE-004
from: root
to: freetier-fe
type: task
priority: critical
status: READ
ref: MSG-TESTER-040-BLOCKED
created: 2026-04-24
---

# FREETIER-FE-004 — Nesting response adapter (BUG-FT-005)

> **BUG-FT-005:** FE vár `sheets[]` placement tömböt, API más formátumban adja.
> A backend (FREETIER-011) párhuzamosan bővíti a response-t `sheets[]` tömbbel.
> A te feladatod: adaptáld a FE-t az API tényleges response formátumára.

## API response formátum (FREETIER-011 UTÁN)

```json
{
  "sheets": [
    {
      "widthMm": 2800,
      "heightMm": 2070,
      "placements": [
        { "partName": "test", "x": 0, "y": 0, "widthMm": 500, "heightMm": 300, "rotated": false }
      ],
      "usedAreaMm2": 150000,
      "wasteAreaMm2": 5646000,
      "yieldPercent": 2.59
    }
  ],
  "totalPartsPlaced": 2,
  "totalPartsRequested": 2,
  "sheetsUsed": 1,
  "yieldPercent": 12.42,
  "wasteAreaMm2": 5076000,
  "algorithm": "Stub-FFDH",
  "computedAt": "2026-04-24T..."
}
```

## Fix

1. **`src/types/nesting.ts`** (vagy ahol a NestingResult type van) — frissítsd az API response type-ot:
   - `sheets: SheetResult[]` — tömb per-sheet placement-ekkel
   - `totalPartsPlaced`, `totalPartsRequested`, `sheetsUsed` (camelCase!)
   - `yieldPercent` (nem `total_utilization_percent`)

2. **`src/api/nestingApi.ts`** — a `fromApiResponse()` adaptert frissítsd az új mezőkre

3. **SVG vizualizáció** — `SheetSvg` component a `sheets[].placements[]` alapján rajzoljon (x, y, widthMm, heightMm, rotated)

4. **Stat cards** — `yieldPercent`, `wasteAreaMm2`, `sheetsUsed`, `totalPartsPlaced` mezőkből

5. **Graceful fallback** — ha `sheets[]` üres vagy hiányzik, a stat cards akkor is mutassák a flat summary-t

## Tesztek (+3)

1. NestingResult type adapter: API camelCase → belső type mapping
2. SVG: sheets[].placements[] renderelés
3. Graceful fallback: sheets[] nélkül is renderel stat cards

## Definition of Done

- [ ] NestingResult type egyezik az API response-szal
- [ ] SVG vizualizáció a placements[] alapján rajzol
- [ ] Stat cards helyes értékeket mutatnak
- [ ] Graceful fallback ha nincs sheets[]
- [ ] `npm run build` 0 error
- [ ] `npm run test` ≥ 71 pass
- [ ] Outbox DONE
