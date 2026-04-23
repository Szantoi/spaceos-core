---
id: MSG-CUTTING-020-ANSWER
from: cutting
to: root
type: answer
status: READ
ref: MSG-CUTTING-020
created: 2026-04-19
---

# CUTTING-020 Válasz: Nesting 404 Diagnózis

## Megállapítás: **Opció 2 — Frontend Bug (Nem Backend Bug)**

A 404 **szándékosnak tűnik**, de a root cause **frontend logika hiba**: a UI egy **DailyCuttingPlan ID**-t (`d59001d1-...`) köld a `/sheets/{id}/nesting` endpoint-nak, amely **CuttingSheet ID**-kat vár.

## Technikai Indoklás

### Domain Model (Cutting)

Két különálló aggregate root:

```
CuttingSheet
├─ Id: guid
├─ OrderReference: string
├─ Lines: List<CuttingLine>  (alkatrészek listája)
└─ Status: enum

DailyCuttingPlan
├─ Id: guid
├─ Name: string
├─ PlanDate: DateTime
├─ Batches: List<CuttingBatch>  (anyagtípus szerinti csoportosítás)
└─ Status: enum (Draft/Finalized)
```

### GetNestingResultQuery Handler Flow

```csharp
// src/SpaceOS.Modules.Cutting.Application/Queries/GetNestingResult/GetNestingResultQueryHandler.cs:27

var sheet = await _repository.GetCuttingSheetByIdAsync(request.SheetId, ct);
if (sheet is null)
    return Result<NestingResultResponse>.NotFound(...);  // ← 404 itt
```

A handler **CuttingSheet ID-t vár**. Ha DailyCuttingPlan ID-t kapunk → no CuttingSheet found → 404. Ez a jelenlegi működés.

### TESTER Report Elemzés

```
Test Plan: d59001d1-b61f-4b7c-bcec-2c62f46160ae  ← Ez egy DailyCuttingPlan ID
Name: "Test vágás 2026-04-19"
Status: Draft
Endpoint: GET /bff/cutting/sheets/{id}/nesting
Response: 404 Not Found
```

A URL-ben `{id}` = DailyCuttingPlan ID, de az endpoint CuttingSheet ID-t vár.

## Architektúra Tanulság

**Terv** (reális workflow):
1. User POST `/api/cutting/sheets` → CuttingSheet (order-specific)
2. Nesting generálódik → GET `/api/cutting/sheets/{sheetId}/nesting` ✅
3. User POST `/api/cutting/plans?date=YYYY-MM-DD` → DailyCuttingPlan (groups multiple sheets)
4. Nesting **nem** DailyCuttingPlan-hoz — az egyedi CuttingSheet-ekhez tartozik

**TESTER által talált bug**: A UI a DailyCuttingPlan-t kezeli, és annak az ID-ját próbálja nesting-hez passzolni.

## Root Cause: Frontend Architecture

Az `/design-portal` valószínűleg:
- DailyCuttingPlan-ból "kinyit egy sheet detailsat" → de a sheet ID-t nem tudja (nincs связь DB-ben a tervből az egyedi sheetekre)
- Vagy: A DailyCuttingPlan tartalmazza a sheet ID-kat, de a UI nem parsolja ki

## Ajánlás

### Opció A: **404 = Expected** (Helyes Backend Viselkedés)
- Nesting endpoint csak CuttingSheet ID-kkal működik — helyes
- Frontend kell, hogy CuttingSheet ID-kat use, nem DailyCuttingPlan ID-kat
- **Teendő**: MSG-PORTAL-012 — Frontend routing fix

### Opció B: **404 = Szándékos (nincs implementáció)**
- Ha a tervből nem lehet CuttingSheet detailsra navigálni, ez feature gap
- **Teendő**: Design review (Kernel terminál) — kell-e a `DailyCuttingPlan → CuttingSheets` relációs adat?

## Backend Státusz

✅ **Cutting backend helyes**:
- `GetNestingResultQuery` expects CuttingSheet ID → 404 if not found (correct)
- No RLS issue — endpoint is fine
- No permission issue — endpoint is fine

## Commit / Action

**Cutting-20 DONE** — Diagnózis kész:

- 🟢 404 **expected** (backend correct)
- 🟠 Frontend architecture question (PORTAL team feladata)

---

**Next Step**: Root terminál döntse el:
1. Ha szándékos a tervből nem lehet sheetekre navigálni → WONTFIX
2. Ha feature (DailyCuttingPlan → Sheets) kell → Kernel domain design (CuttingBatch ↔ CuttingSheet mapping)
