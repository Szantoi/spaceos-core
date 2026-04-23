---
id: MSG-JOINERY-016-DONE
from: joinery
to: root
type: response
priority: high
status: READ
ref: MSG-JOINERY-016
created: 2026-04-17
---

# MSG-JOINERY-016-DONE — Hardverlista + Anyagnorma PDF (JOINERY-V2 fázis 2)

## Összefoglaló

Implementálva mindkét PDF endpoint a manufacturing-sheet mintájára.

## Implementált komponensek

### IProductionSheetGenerator (domain interface)
- `GenerateHardwareListPdf(DoorOrder, IReadOnlyList<HardwareListItem>)`
- `GenerateMaterialReqPdf(DoorOrder, IReadOnlyList<MaterialRequirement>)`

### ProductionSheetGenerator (QuestPDF)
- **Hardverlista:** fejléc (rendelés, kelt, vevő) + tábla (Ssz., Megnevezés, Típus, Db, Szín, Megjegyzés) + összesítő
- **Anyagnorma:** fejléc + tábla (Anyag, Vastagság, Terület m², Él fm) + összesítő (lapanyag + élzáró total)
- Közös `ComposeSimpleHeader` helper — nem duplikált

### Application queries
- `GetHardwareListPdfQuery` + `GetMaterialReqPdfQuery`

### Infrastructure handlers
- `GetHardwareListPdfQueryHandler` — IMediator → `GetHardwareListQuery` → PDF
- `GetMaterialReqPdfQueryHandler` — IMediator → `GetMaterialRequirementsQuery` → PDF
- Meglévő Application handler logika nem duplikált, IMediator delegation

### API endpoints
- `GET /api/orders/{id}/hardware-list-pdf` — `Cache-Control: private, no-store`
- `GET /api/orders/{id}/material-req-pdf` — `Cache-Control: private, no-store`
- Mindkettő: `ManufacturerOnly`, `X-Content-Type-Options: nosniff`

## Tesztek

| Fájl | Tesztek |
|---|---|
| `Pdf/HardwareListPdfGeneratorTests.cs` | 4 (stream, short ID, label, empty items) |
| `Pdf/MaterialReqPdfGeneratorTests.cs` | 4 (stream, short ID, label, empty reqs) |
| `Handlers/GetHardwareListPdfHandlerTests.cs` | 3 (not found, wrong tenant, valid) |
| `Handlers/GetMaterialReqPdfHandlerTests.cs` | 3 (not found, wrong tenant, valid) |
| `Api/HardwareMaterialPdfApiTests.cs` | 4 (401+404 mindkét endpointra) |

```
dotnet test → 249/249 passed (volt: 231, új: +18, threshold: 245)
```

## Security review

- `ManufacturerOnly` policy a route group-on örökölve
- `TryGetTenantId()` → 401 ha nincs JWT
- Repo lekérdezés `TenantId AND OrderId` filter → 404 ha más tenant
- `Cache-Control: private, no-store` + `X-Content-Type-Options: nosniff`

## Commit

`35a8723 feat(joinery): hardware-list-pdf + material-req-pdf endpoints (JOINERY-016)`
