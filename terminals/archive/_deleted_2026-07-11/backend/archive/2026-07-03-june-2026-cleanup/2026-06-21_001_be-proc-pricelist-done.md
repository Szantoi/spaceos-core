---
id: MSG-BACKEND-001-DONE
from: backend
to: conductor
type: done
status: READ
ref: MSG-BACKEND-001
created: 2026-06-21
content_hash: 74289d8366a56d8a3851e4ac61953f49d03e2e6db68495f4c1b1359b66040e3a
---

## Összefoglaló

**BE-PROC-001: Beszállítói önkiszolgáló árlista API** — implementálva és tesztelve.

Implementált funkciók:
- **Domain layer**: `PriceList.Update()` metódus (csak Draft állapotban szerkeszthető)
- **Application layer**:
  - `UpdatePriceListCommand` + Handler + Validator
  - `GetPriceListsBySupplierQuery` + Handler
  - Auto-expire logika az `ActivatePriceListCommandHandler`-ben
- **Infrastructure layer**:
  - `GetPriceListsBySupplierAsync()` repository metódus
  - `GetActivePriceListsBySupplierAsync()` repository metódus
- **API layer**: 4 új supplier-scoped endpoint
  - POST `/api/procurement/suppliers/{supplierId}/price-list`
  - PUT `/api/procurement/suppliers/{supplierId}/price-list/{id}`
  - POST `/api/procurement/suppliers/{supplierId}/price-list/{id}/activate`
  - GET `/api/procurement/suppliers/{supplierId}/price-list`

## Implementált üzleti szabályok

✅ Egy beszállítónak egyszerre csak EGY aktív árlistája lehet
✅ Aktiváláskor az előző aktív árlista automatikusan Expired lesz (auto-expire logika)
✅ A beszállító csak a SAJÁT árlistáját kezelheti (tenant + supplier ID ellenőrzés)
✅ Csak Draft státuszú árlista módosítható (Update guard)

## Tesztek

**Build:** ✅ PASS (0 error, 0 warning)

**Unit tesztek:** ✅ PASS (10/10 PriceList domain tests)
- `Update_FromDraft_ShouldSucceed`
- `Update_FromActive_ShouldFail`
- `Update_FromExpired_ShouldFail`
- `Update_WithInvalidDateRange_ShouldFail`
- `Update_WithNoEntries_ShouldFail`
- + 5 eredeti PriceList teszt

**Full test suite:** 140 passed, 1 failed (InternalReceiverTests - nem kapcsolódik a feladathoz)

**Coverage:** Az új domain Update metódus és FSM átmenetek 100%-ban le vannak fedve unit tesztekkel.

## Security checklist

✅ Input validation (FluentValidation az `UpdatePriceListValidator`-ban)
✅ Authorization (tenant isolation + `RequireAuthorization("ManufacturerOnly")`)
✅ RLS policy (nem érintett, a meglévő `PriceLists` tábla már RLS-szel védett)
✅ Paraméteres query (EF Core LINQ, nincs string concatenation)
✅ Sensitive data nem kerül logba

## Módosított fájlok

**Domain (1):**
- `SpaceOS.Modules.Procurement.Domain/Aggregates/PriceList.cs` (+29 sor)

**Application (4):**
- `Commands/UpdatePriceList/UpdatePriceListCommand.cs` (új)
- `Commands/UpdatePriceList/UpdatePriceListValidator.cs` (új)
- `Commands/UpdatePriceList/UpdatePriceListCommandHandler.cs` (új)
- `Commands/ActivatePriceList/ActivatePriceListCommandHandler.cs` (+10 sor auto-expire)
- `Queries/GetPriceListsBySupplier/GetPriceListsBySupplierQuery.cs` (új)
- `Queries/GetPriceListsBySupplier/GetPriceListsBySupplierQueryHandler.cs` (új)

**Infrastructure (2):**
- `Domain/Interfaces/IProcurementV2Repository.cs` (+2 metódus)
- `Infrastructure/Repositories/ProcurementV2Repository.cs` (+19 sor)

**API (1):**
- `Api/Endpoints/PriceListEndpoints.cs` (+80 sor, 4 új endpoint + DTO-k)

**Tests (1):**
- `Tests/Domain/PriceListTests.cs` (+5 új teszt)

## Kockázatok

⚠️ **1 teszt fail** (`InternalReceiverTests.PostFromReorderAlert_OrphanMaterialCode_Returns422`) — nem kapcsolódik a mostani feladathoz, előzőleg már failing volt. Külön fix-re szorul.

## Következő lépések (opcionális)

- Integration tesztek a handler-ekhez (mock repository-val)
- E2E teszt a full supplier self-service flow-ra
- Swagger dokumentáció review (OpenAPI schema ellenőrzése)
