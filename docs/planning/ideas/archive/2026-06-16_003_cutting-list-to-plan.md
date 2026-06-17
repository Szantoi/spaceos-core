---
domain: manufacturing
type: ux_gap
priority: high
created: 2026-06-16
scanned_by: haiku
---

# Design → Cutting Plan Workflow

## Mit old meg
Tervez egy ajtót DesignPage-en (paraméter form), generálja az anyaglistát, de aztán az alkatrészek "eltűnnek". Nincs UI-ban látható link hogy "ez a design → ezek a vágási lapok → ezek futnak ma a gépen".

## Jelenlegi állapot
- **DesignPage (MaterialsGenerator):**
  - Step 3: Generált alkatrészlista ✅
  - Step 4: "Szabászlistába küldve" toast + cutting plan ID (pl. `CP-184-ABC`)
  - De ezután már nincs UI callback — operátor kézi kell megkeresse a Production-ben
  - `POST /abstractions/api/modules/templates/{id}/cutting-list` nem mentés, csak preview

- **ProductionPage:**
  - Vágási tervek lista (`GET /cutting/api/cutting/plans`) betöltödik
  - De nincs link vissza a design-hoz vagy az originating quote-hoz
  - `displayId` csak CP-184-ABC szöveg, nincs customer/order context

## Bekötési lehetőség

1. **DesignPage módosítás:**
   - Step 4 submit button: `POST /cutting/api/sheets` (helyett a mock send-nek)
   - Request body: order ref (KJ-2426-0184) + design template ID + calculated parts lista
   - Response: sheet ID + cutting plan ID
   - Callback: navigate to Production page + highlight új plan-t

2. **CuttingSheet API endpoint:**
   - Backend: `POST /cutting/api/sheets` — már létezik (`SubmitCuttingSheet` command)
   - Request: orderReference, lines (partName, materialType, dimensions, qty)
   - Response: sheetId, nestingResult (optional inline)

3. **ProductionPage context:**
   - Plan row: customer name, order ID, design template, parts count
   - Click → expand OrderContext (melyik design-ből jött)
   - Breadcrumb: Design > Material List > Nesting > Execution

## Iparági relevancia
Doorstar workflow: Ügyfél → Quote → Design → Material List → Cutting → Execution → Delivery. Jelenleg egy "szaggatott" UI (Viber → Design → SMS → ManuálisKézzel). Integrált workflow = kevesebb félreértés, gyorsabb production ramp.
