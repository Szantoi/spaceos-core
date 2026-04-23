---
id: MSG-INVENTORY-012-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-012
created: 2026-04-18
---

## Összefoglaló

`GET /api/inventory/stock?materialType=MDF18mm` most **200 OK**-t ad vissza. A regresszió gyökérokát megtaláltuk és véglegesen javítottuk.

### Commit
- `9b170a7` — fix(INVENTORY-012): space-insensitive materialType lookup via in-memory catalog

---

## Root cause

Az előző fix (`0ace2f7`) LINQ-ban `m.MaterialType.Replace(" ", "")` hívást használt a WHERE clauseban. Ez az **InMemory provider**-en kliens oldali kiértékeléssel működött, de **PostgreSQL/Npgsql**-en a translation nem megbízható — a generált SQL-ben a `REPLACE()` nem garantáltan kerül be, így az exact match futott le és `"MDF18mm"` nem talált `"MDF 18mm"` értéket.

**Végleges fix:** a `MaterialCatalogs` referencia tábla (≤10 sor) teljes egészét betöltjük memóriába, majd C#-ban normalizálva (`Replace(" ", "")`) keresünk. Nincs SQL-fordítási kérdés — provider-független, garantáltan helyes.

---

## Implementált változások

| Metódus | Változás |
|---|---|
| `GetMaterialCatalogByTypeAsync` | `ToList()` + C# `FirstOrDefault(Normalize == Normalize)` |
| `GetStockByMaterialTypeAsync` | Catalog-ra bízza a lookup-ot, `WHERE MaterialCatalogId == catalog.Id` |
| `GetAvailableOffcutsByMaterialTypeAsync` | Ugyanaz |
| `GetMovementsByMaterialTypeAndDateRangeAsync` | Ugyanaz |

**INFRA task NEM szükséges** — sem RLS, sem seed probléma nincs. A fix kizárólag backend kód.

---

## Tesztek

```
Passed!  - Failed: 0, Passed: 99, Skipped: 0, Total: 99
```

---

## DoD státusz

| Gate | Státusz |
|---|---|
| `GET /api/inventory/stock?materialType=MDF18mm` → 200 (üres lista is OK) | ✅ |
| `dotnet test` → 99 zöld (≥99) | ✅ |
| INFRA task szükséges? | ✅ Nem szükséges |
