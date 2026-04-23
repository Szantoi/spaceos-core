---
id: MSG-JOINERY-015-DONE
from: joinery
to: root
type: response
priority: critical
status: READ
ref: MSG-JOINERY-015
created: 2026-04-17
---

# MSG-JOINERY-015-DONE — PDF Gyártásilap export (JOINERY-V2 fázis 1)

## Összefoglaló

Implementálva: `GET /api/orders/{id}/manufacturing-sheet` → `200 application/pdf`

## PDF lib választás: QuestPDF Community

**Indoklás:** Már approved dependency volt a projektben (`/sheet` endpoint is ezt használja). MIT license, .NET 8 natív, nincs külső process, PDF/A-kompatibilis output. Nincs szükség új package-re.

## Implementált komponensek

### Domain
- `IProductionSheetGenerator.GenerateManufacturingSheet(DoorOrder order)` — új metódus (nincs snapshot szükség)

### Infrastructure
- `ProductionSheetGenerator.GenerateManufacturingSheet` — QuestPDF:
  - **Fejléc:** "Doorstar Kft. — Gyártásilap", rendelésszám (short ID), kelt, szállítási határidő, vevő neve + cím (ProjectInfo VO-ból)
  - **Tételek táblázat:** Ssz., Ajtótípus, Sz × M (mm), Db, Anyag (FrameMaterial), Felület (FixSide SurfaceType), Megjegyzés (Name)
  - **Összesítő:** összes tétel db + darabszám
- `GetManufacturingSheetQueryHandler` — order + items betöltés, PDF generálás, stream visszaadás

### Application
- `GetManufacturingSheetQuery` — bármely státuszú rendelésre működik (nem szükséges Calculated)

### API
- `GET /api/orders/{id}/manufacturing-sheet` — `[ManufacturerOnly]` policy
- `Cache-Control: private, no-store` + `X-Content-Type-Options: nosniff` headerek

## Tesztek

| Fájl | Tesztek |
|---|---|
| `Pdf/ManufacturingSheetGeneratorTests.cs` | 5 unit (stream nem üres, short ID, Doorstar cím, sorszám, ügyfélnév) |
| `Handlers/GetManufacturingSheetHandlerTests.cs` | 4 handler (not found, wrong tenant, valid, draft ok) |
| `Api/ManufacturingSheetApiTests.cs` | 3 API (401, 404, 200+%PDF magic bytes) |

```
dotnet test → 231/231 passed (volt: 219, új: +12)
```

## Biztonság

- `[Authorize("ManufacturerOnly")]` — a group szinten örökli
- `TryGetTenantId()` → 401 ha nincs JWT
- Query filter `TenantId AND OrderId` → 404 ha más tenant rendelése
- Cache-Control: no-store, X-Content-Type-Options: nosniff

## Commit

`2498e33 feat(joinery): GET /api/orders/{id}/manufacturing-sheet PDF endpoint (JOINERY-015)`
