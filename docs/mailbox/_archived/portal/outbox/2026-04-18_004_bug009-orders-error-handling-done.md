---
id: MSG-PORTAL-004-DONE
from: portal
to: root
type: done
status: READ
ref: MSG-PORTAL-004
created: 2026-04-18
---

## Összefoglaló

BUG-009 — Rendelések és Készlet UI hiányzó error handling: javítva.

## Módosítások

| Fájl | Változás |
|---|---|
| `CreatePurchaseOrderModal.tsx` | `isError` kinyerve a mutációból; piros `role="alert"` banner POST failure esetén |
| `ProcurementPage.tsx` | `isError` kinyerve a lekérdezésből; piros `role="alert"` banner GET failure esetén |
| `InventoryPage.tsx` | `stockError` kinyerve; `stockError \|\| !stock` → szaggatott szürke üres állapot (nem piros hiba) |
| `CreatePurchaseOrderModal.test.tsx` | **ÚJ fájl** — 5 teszt (form validáció, error banner, pending, cancel) |
| `ProcurementPage.test.tsx` | +1 teszt: GET error state; meglévő mock-ok `isError: false`-szal egészítve |
| `InventoryPage.test.tsx` | +1 teszt: 404/error → üres állapot, nincs `role="alert"` |

## Tesztek

318 / 318 zöld (+7 új teszt).

## Security review

- XSS: hibaüzenetek hardcoded string literálok, nem server response tartalom
- Auth: változatlan
- Input sanitization: zod validáció megmarad
- OWASP: stack trace nem szivárog UI-ra

## Megjegyzés

INFRA deploy szükséges (DoD szerint). Portal oldalon kész.

## Commit

`3cf19de` — fix(ui): error handling for Procurement + Inventory (BUG-009)
