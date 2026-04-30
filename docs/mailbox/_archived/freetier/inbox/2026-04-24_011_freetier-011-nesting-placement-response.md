---
id: MSG-FREETIER-011
from: root
to: freetier
type: task
priority: critical
status: READ
ref: MSG-TESTER-040-BLOCKED
created: 2026-04-24
---

# FREETIER-011 — Nesting API response bővítés (placement koordináták)

> **BUG-FT-005:** Az API stub nem ad `sheets[]` placement tömböt → a frontend SVG vizualizáció üres.

## Jelenlegi response

```json
{
  "sheetCount": 1,
  "totalPartCount": 2,
  "yieldPercent": 12.42,
  "wasteAreaMm2": 5076000,
  "algorithm": "Stub-FFDH",
  "computedAt": "2026-04-24T..."
}
```

## Elvárt response

```json
{
  "sheets": [
    {
      "widthMm": 2800,
      "heightMm": 2070,
      "placements": [
        {
          "partName": "test",
          "x": 0,
          "y": 0,
          "widthMm": 500,
          "heightMm": 300,
          "rotated": false
        }
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

## Implementáció

A `NestingEngineService` stub-ot bővítsd egyszerű FFDH elhelyezéssel:
1. Sheet-re sor-rendben pakolja az alkatrészeket (balról jobbra, soronként)
2. Ha nem fér: következő sor, ha a sor sem fér: következő sheet
3. Minden placement-nek legyen `x`, `y`, `widthMm`, `heightMm`, `rotated`
4. A response tartalmazza a `sheets[]` tömböt a placement-ekkel

**DTO:** Hozz létre `NestingResultDto` osztályt ami tartalmazza a `sheets[]` + összesítő mezőket.

## Tesztek (+3)

1. Nesting 1 sheet, 2 part → sheets[0].placements.length == 2
2. Placement x/y koordináták nem negatívak és sheet-en belül vannak
3. Több sheet: ha nem fér 1 sheet-re → sheetsUsed > 1

## Definition of Done

- [ ] `POST /nest` response tartalmaz `sheets[]` tömböt placement koordinátákkal
- [ ] Stub FFDH: alkatrészek sor-rendben elhelyezve
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` ≥ 173 pass
- [ ] Outbox DONE
