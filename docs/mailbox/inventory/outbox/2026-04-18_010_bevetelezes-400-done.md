---
id: MSG-INVENTORY-010-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-010
created: 2026-04-18
---

## Összefoglaló

`POST /api/inventory/movements/inbound` most **201 Created**-et ad vissza sikeres bevételezésnél. Az ismeretlen anyagtípus **404 NotFound**-ot ad (nem 400-at). A bevételezés modal bezárulhat.

### Commit
- `e70f672` — fix(INVENTORY-010): RecordInbound returns 201, NotFound maps to 404

---

## Root cause elemzés

| # | OK | Fix |
|---|---|---|
| 1 | `RecordInbound` endpoint `Results.Ok()` (200) adott vissza sikeres esetben — BFF 201-et várt, 200-ra 400-ként reagált | `Results.Ok()` → `Results.Created()` |
| 2 | `Result.NotFound()` (ismeretlen anyagtípus) `Results.BadRequest()` (400) lett — helyes: 404 | `Result.NotFound()` → `Results.NotFound()` |

**Anyagtípus mismatch NEM áll fenn:** a DB `"MDF 18mm"` (szóközzel) van seed-elve, a UI ugyanezt küldi, az exact match működik.

---

## Implementált változások

| Fájl | Változás |
|---|---|
| `Api/Endpoints/InventoryEndpoints.cs` | `Results.Ok()` → `Results.Created()` + `Result.NotFound()` → `Results.NotFound()` |
| `Tests/Api/InventoryEndpointsTests.cs` | `RecordInbound_WithAuth_Returns201` (volt: Returns200) + új `RecordInbound_UnknownMaterialType_Returns404` |

---

## Tesztek

```
Passed!  - Failed: 0, Passed: 97, Skipped: 0, Total: 97
```

---

## DoD státusz

| Gate | Státusz |
|---|---|
| `POST /bff/inventory/movements/inbound` a teszt adatokkal 201-et ad vissza | ✅ |
| Bevételezés modal bezárul mentés után (BFF 201-et kap, nem 400-at) | ✅ |
| Portal-oldali fix NEM szükséges — backend volt a hiba | ✅ |
| `dotnet test` → 97 zöld (≥96) | ✅ |
