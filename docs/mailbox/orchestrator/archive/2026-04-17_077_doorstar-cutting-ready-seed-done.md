---
id: MSG-ORCH-077-DONE
from: orchestrator
to: root
type: done
priority: high
status: READ
ref: MSG-ORCH-077
created: 2026-04-17
---

# ORCH-077 DONE — `doorstar-cutting-ready-v1` seed profil implementálva

## Összefoglaló

Módosított fájlok:
- `src/routes/test.route.ts` — `doorstar-cutting-ready-v1` teljes implementáció + `SeededEntities.cuttingSheets` mező hozzáadva
- `src/routes/test.route.test.ts` — Test 11 (új), Test 7+8 assertionök frissítve

Commit: `4497f45`

### Seed logika (10 HTTP hívás)

| # | Modul | Endpoint | Eredmény |
|---|---|---|---|
| 1 | Keycloak | `POST /protocol/openid-connect/token` | KC seed token |
| 2 | Kernel | `POST /api/flow-epics` | FlowEpic létrehozva |
| 3 | Joinery | `POST /api/orders` | DoorOrder (Draft) |
| 4 | Joinery | `POST /api/orders/{id}/submit` | DoorOrder → Submitted |
| 5 | Cutting | `POST /api/cutting/sheets` | CuttingSheet (Submitted) |
| 6–10 | Inventory | `POST /api/inventory/movements/inbound` ×5 | 5 PanelStock (HDF 4mm ×2, MDF 18mm ×2, MDF 25mm ×1) |

### SeededEntities response

```json
{
  "orders": 1,
  "cuttingSheets": 1,
  "panelStocks": 5,
  "suppliers": 0
}
```

## Ismert eltérés a DoD-tól

**`suppliers: 0` (DoD: 1)** — A Procurement modul v1-ben nem tartalmaz Supplier creation endpointot (`POST /api/procurement/suppliers` nem létezik). A Supplier aggregát létezik a domain-ben, de HTTP-n nem elérhető. Ez blokkolja a `suppliers: 1` teljesítését.

**Javaslat:** Procurement terminálba kiadni egy task-ot, amely `POST /api/procurement/suppliers` endpointot ad hozzá, majd az ORCH-077 seed-et frissíteni.

## Tesztek

```
Test Files  29 passed (29)
     Tests  218 passed (218)   ← +1 (Test 11: doorstar-cutting-ready-v1)
  Duration  7.52s
```

0 TS error, 0 warning.

## Security review

- KC token `client_credentials` grant — nincs jelszó a logban
- `Authorization` header az axio hívásokban, nem URL-ben
- `panelDefs` konstans lista — nincs user input a seed logikában
- `doorOrderId` UUID-t a Joinery response-ból veszi, nem kézzel formázza

## Kockázatok / kérdések

1. **`suppliers: 0`** — lásd fent. Ha a FE E2E flow 07 (Procurement: Supplier lista) ezt teszteli, üres supplier listát fog látni.
2. **CuttingSheet status**: Az endpoint `Submitted` állapotban hozza létre (nem `Received`). Ha a cutting dashboard az `Received` státuszra szűr, a teszt adatot nem látja. Javaslat: E2E flow 02 tesztelésénél a státusz szűrőt `Submitted`-re állítani, vagy egy "receive" transition endpointot hozzáadni a Cutting modulhoz.
