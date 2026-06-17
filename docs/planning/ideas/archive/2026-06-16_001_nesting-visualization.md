---
domain: manufacturing
type: endpoint_gap
priority: high
created: 2026-06-16
scanned_by: haiku
---

# Nesting Visualizáció — ProductionPage

## Mit old meg
Gyártó műhelyében az operátor nézheti meg a vágási tervet egy 2D nézetben, hogy pont megértse mely panelok kerülnek vágásra és mekkora a hulladekat az optimalizáció után. Ez a Doorstar workflow-jában kritikus (4. lépés: Execution planning).

## Jelenlegi állapot
- **Backend:** `GET /cutting/api/cutting/sheets/{id}/nesting` létezik (GetNestingResultQuery)
- **Frontend:** ProductionPage nesting viewer csak mock: _"Nesting API nem elérhető"_
- **NuGet:** `spaceos-nesting-algorithms` modul képes Guillotine + FFDH stratégiákat futtatni, teszt cover 32/32 ✅

## Bekötési lehetőség
1. ProductionPage `<Card className="col-span-8 p-5">` (172. sor) nesting viewer:
   - `const { data: nestingResult } = useApi(`${API_BASE.cutting}/api/cutting/sheets/{selectedPlanId}/nesting`)`
   - SVG canvas: panel + placed parts 2D vizualizáció (scale-zett)
   - Waste percentage badge (pl. 12% hulladeák)
   - Material type filter + color coding

2. Material color scheme: már létezik Design > Catalog-ban (CATALOG_LOOKUP)

3. Nesting plan detail:
   - Sheets count
   - Strategy (Guillotine / FFDH)
   - Total waste %
   - Per-sheet placement vizualizáció toggle

## Iparági relevancia
Asztalosok / ajtógyártók napi szinten nézi a vágási terv gazdaságosságát előtt a gép indul. A Doorstar terv 2-3 panel naponta, így azonnali láthatóság kritikus. Nesting optimization (waste ↓) = anyag megtakarítás = profit.
