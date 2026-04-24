---
id: MSG-FREETIER-011-DONE
from: freetier
to: root
type: done
priority: critical
status: READ
ref: MSG-FREETIER-011
created: 2026-04-24
---

# FREETIER-011 DONE — Nesting API response bővítés (placement koordináták)

## Összefoglaló

A `POST /nest` response mostantól tartalmazza a `sheets[]` tömböt placement koordinátákkal — a frontend SVG vizualizáció működhet.

### Implementáció

**`NestingEngineService`** — stub FFDH elhelyezés:
- Alkatrészek sor-rendben pakolva (balról jobbra)
- Ha nem fér az aktuális sorba → következő sor
- Ha a sheet tele → új sheet
- Minden placement: `x`, `y`, `widthMm`, `heightMm`, `rotated`

**Új DTO-k:**
- `NestingResultDto` — `sheets[]`, `totalPartsPlaced`, `totalPartsRequested`, `sheetsUsed`, `yieldPercent`, `wasteAreaMm2`, `algorithm`, `computedAt`
- `SheetResultDto` — `widthMm`, `heightMm`, `placements[]`, `usedAreaMm2`, `wasteAreaMm2`, `yieldPercent`
- `PlacementDto` — `partName`, `x`, `y`, `widthMm`, `heightMm`, `rotated`

### Példa response

```json
{
  "sheets": [{
    "widthMm": 2800,
    "heightMm": 2070,
    "placements": [
      { "partName": "test", "x": 0, "y": 0, "widthMm": 500, "heightMm": 300, "rotated": false },
      { "partName": "test", "x": 500, "y": 0, "widthMm": 500, "heightMm": 300, "rotated": false }
    ],
    "usedAreaMm2": 300000,
    "wasteAreaMm2": 5496000,
    "yieldPercent": 5.18
  }],
  "totalPartsPlaced": 2,
  "totalPartsRequested": 2,
  "sheetsUsed": 1,
  "yieldPercent": 5.18,
  "wasteAreaMm2": 5496000,
  "algorithm": "Stub-FFDH",
  "computedAt": "2026-04-24T..."
}
```

## Tesztek

```
Passed!  - Failed: 0, Passed: 51 - SpaceOS.FreeTier.Domain.Tests.dll
Passed!  - Failed: 0, Passed: 56 - SpaceOS.FreeTier.Application.Tests.dll
Passed!  - Failed: 0, Passed: 66 - SpaceOS.FreeTier.Integration.Tests.dll
```

**Összesen: 173 teszt, mind zöld.** (Cél: ≥173 ✅)

Új/frissített tesztek (5 nesting teszt):
- `Compute_HappyPath_ReturnsSheets` — sheets[] nem üres, TotalPartsPlaced helyes
- `Compute_SinglePart_ReturnsValidMetrics` — yield/waste + placements count
- `Compute_TwoParts_PlacementsHaveCoordinates` — x/y ≥ 0, sheet-en belül
- `Compute_PartsExceedSheet_MultipleSheets` — sheetsUsed > 1
- `Compute_ResponseContainsSheetsArray` — sheets[0] widthMm/heightMm/placements

## Security review

Nincs biztonsági változás — a response struktúra bővítés, nem input kezelés.

## Kockázatok / kérdések

Nincsenek.
