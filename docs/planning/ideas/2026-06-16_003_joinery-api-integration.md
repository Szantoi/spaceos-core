---
domain: manufacturing
segment: joinery-memory
type: endpoint_gap
priority: high
created: 2026-06-16
---

# Joinery API Integration — Material Requisition & Daily Cutting Plan

## Mit old meg

A Joinery modul (port 5002) már kínál kritikus manufacturing backend API-kat:
- `GET /api/orders/{id}/process-plan` — termelési eljárás lépések
- `GET /api/orders/{id}/material-req` — anyagszükséglet (BOM)
- `GET /api/orders/{id}/hardware-list` — ferdeség, festékadatok
- `POST /api/cutting/plans` — napi vágási terv generálása

De a **frontend (Portal) még nem integrálódik ezekre** — DesignPage és ProductionPage mock-data-val dolgozik. Ezt az integráció-gap-ot bezárva:
- A gyártók **valós anyaglistákat** látnak (költség, raktár-szint)
- A termelést **paraméterezhetik** (ferdeség, vágási sorrend)
- A napi terv **automata generálódik** cutting planes-ről

## Jelenlegi állapot

| Component | Status | Gap |
|-----------|--------|-----|
| **Joinery Backend** | ✅ Ready | `/api/orders/{id}/material-req`, `/api/orders/{id}/hardware-list` endpoints implemented (E7-JOINERY) |
| **Cutting Backend** | ✅ Ready | POST `/api/cutting/plans`, daily plan execution FSM (E7-CUTTING) |
| **Portal Frontend** | ❌ Incomplete | `DesignPage`: hardver-lista = mock (JSON hardcoded); `ProductionPage`: daily plans = mock |
| **Integration** | ❌ Missing | Frontend hooks + API calls missing; orchestrator routing needs verify |

## Bekötési lehetőség

### 1. Frontend Hook (2 óra)
```
OrderDetailPage.tsx:
  + useEffect(() => fetchMaterialReq(orderId)) → API call to GET /api/orders/{id}/material-req
  + useEffect(() => fetchHardwareList(orderId)) → GET /api/orders/{id}/hardware-list
  + Render: <MaterialRequisitionTable /> + <HardwareSpecsCard />

ProductionPage.tsx:
  + Button: "Generate Daily Cutting Plan" → POST /api/cutting/plans { date, capacity, orders[] }
  + Poll: GET /api/cutting/plans?date={today} → render CuttingPlanTable
```

### 2. Orchestrator API Routes (1 óra)
Verify that `/api/orders/*` and `/api/cutting/*` route correctly to Joinery (5002) and Cutting (5004) from Orchestrator (3000):
```
GET /api/orders/{id}/material-req → router.get('/api/orders/:id/material-req', joinery_proxy)
GET /api/orders/{id}/hardware-list → router.get('/api/orders/:id/hardware-list', joinery_proxy)
POST /api/cutting/plans → router.post('/api/cutting/plans', cutting_proxy)
GET /api/cutting/plans → router.get('/api/cutting/plans', cutting_proxy)
```

### 3. Integration Tests (1.5 óra)
E2E: OrderDetail → fetch material-req → verify rendering; ProductionPage → POST cutting plan → verify poll returns result

## Iparági relevancia

- **MES Standard** (MESA, ISA-95): material planning + BOM management = baseline manufacturing ops
- **Cost Control** — asztalosiparos KKV-k anyag selejtezésre sokszor vakitanak; valós lista → rossz vásárlások csökkentése
- **Doorstar Soft Launch** — termelési paraméterek (ferdeség, anyag) ma Viber-en beszélgetnek; digitális BOM = verseny előny

## Érintett modulok

- **Frontend** (`design-portal`): `OrderDetailPage`, `ProductionPage` hook + component frissítés
- **Orchestrator**: API route config verify
- **E2E tests**: order→material-req→cutting-plan chain

## Blocker

Nincs. Backend API-k ready (E7 modul), csak frontend integráció hiányzik.

## Next: megkötések

**Nem adunk id** a cutting-plan-hoz explicit url param-ben — csak POST { date, orders } és GET query ?date=YYYY-MM-DD.
**Nem kezelünk** batch material-req-et — egy order = egy material-list. Ha multi-order BOM kell, Phase 2.
