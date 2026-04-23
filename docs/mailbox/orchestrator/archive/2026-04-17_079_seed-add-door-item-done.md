---
id: MSG-ORCH-079-DONE
from: orchestrator
to: root
type: done
priority: high
status: READ
ref: MSG-ORCH-079
created: 2026-04-17
---

# ORCH-079 DONE — `doorstar-cutting-ready-v1` seed fix: DoorOrder item hozzáadva

## Összefoglaló

A `doorstar-cutting-ready-v1` seed profil ④ submit lépése előtt hiányzott legalább egy DoorOrder item.
A Joinery domain rule (`_items.Count == 0 → Result.Invalid`) HTTP 400-at adott vissza, amely seed 502-t okozott.

**Változtatott fájlok:**

| Fájl | Változás |
|---|---|
| `src/routes/test.route.ts` | ③b lépés hozzáadva: `POST /api/orders/{id}/items` a submit előtt |
| `src/routes/test.route.test.ts` | Mock + call count frissítve (12 → 13), ③b mock entry hozzáadva |

**Új seed sorrend (`doorstar-cutting-ready-v1`):**
```
① Facility létrehozás (Kernel)
② FlowEpic létrehozás (Kernel)
③ DoorOrder létrehozás (Joinery, Draft)
③b DoorOrder item hozzáadás (Joinery) ← ÚJ
④ DoorOrder submit (Joinery, Draft → Submitted)
⑤ CuttingSheet létrehozás (Cutting)
⑥ 5× PanelStock inbound movement (Inventory)
⑦ Supplier létrehozás (Procurement, graceful)
```

**Item payload:**
```typescript
{
  sorszam: '001', name: 'Seed ajtó', quantity: 1,
  doorType: 'Sikban', openingDirection: 'Left',
  wallOpeningWidth: 920, doorWidth: 900,
  wallOpeningHeight: 2120, doorHeight: 2100,
  wallOpeningThickness: 150, doorThickness: 45,
}
```

## Tesztek

- **218/218 pass** (29 test file)
- Meglévő `doorstar-cutting-ready-v1` teszt frissítve: mock + call count (nem új teszt, helyes frissítés)
- Build: 0 TypeScript error

## Security review

- Seed profil nem kerül prod-ra (testGuard + allowlist védi)
- Az item payload hardcoded seed adat — nincs user input, nincs injection kockázat
- `Authorization: auth` tokent nem logoljuk
- OWASP: nincs új felület, csak belső seed hívás bővült

## Kockázatok / kérdések

Nincs. A fix egyértelmű domain constraint teljesítés.

**Következő lépés:** INFRA-142 — ORCH redeploy + seed pipeline újraverifikáció
