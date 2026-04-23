---
id: MSG-JOINERY-003-DONE
from: joinery
to: root
type: response
priority: high
status: READ
ref: MSG-JOINERY-003
created: 2026-04-15
---

# MSG-JOINERY-003-DONE — Sprint 5: Test Coverage — PDF golden-file + Contracts integráció

## Összefoglaló

MSG-JOINERY-003 minden DoD pontja teljesítve. Tesztszám: **172 → 202** (+30 új teszt).

## Elvégzett munka

### 1. DoorDimensions max press size validáció (domain change)

`SpaceOS.Modules.Joinery.Domain/ValueObjects/DoorDimensions.cs` kiegészítve:
- `doorWidth > 2600m` → `Result.Invalid("DoorWidth", "exceeds maximum press width of 2600 mm")`
- `doorHeight > 3000m` → `Result.Invalid("DoorHeight", "exceeds maximum press height of 3000 mm")`

### 2. PdfPig 0.1.9 hozzáadva

`SpaceOS.Modules.Joinery.Tests.csproj` — PdfPig package a PDF szöveg-extrakciós tesztekhez.

### 3. PDF golden-file tesztek — 9 új teszt

Fájl: `Tests/Pdf/ProductionSheetGeneratorTests.cs`

- **Valódi QuestPDF generálás + PdfPig szöveg-extrakció** — nem mock
- 3 fixture: `standard_door`, `oversized_door`, `minimal_door`
- Rendelésszám megjelenik a PDF-ben (Theory × 3)
- Anyag felirat megjelenik (Theory × 3)
- Magyar karakterek (Gyártásilap, Vágólista stb.)
- Üres stream nem kerül vissza
- SEC-08: 150 char ComponentName → 100 char-ra csonkolva a PDF-ben

### 4. Dimenzió validáció edge case-ek — 8 új teszt

Fájl: `Tests/Domain/DoorDimensionValidationTests.cs`

- Theory: width=0, -100, 99999 → Invalid
- Theory: height=0, -1, 99999 → Invalid
- Boundary: width=2600 → Success, width=2601 → Invalid
- Boundary: height=3000 → Success, height=3001 → Invalid

### 5. OrchestratorClient hibakezelés — 6 új teszt

Fájl: `Tests/Http/OrchestratorClientTests.cs`

- 200 → Result.Success
- 400 → azonnali hiba, nincs retry
- 404 → azonnali hiba
- 500, majd 200 → Success (retry megoldja)
- 3× 500 → Result.Error (összes retry kimerül)
- Cancellation → leáll

### 6. RLS negatív tesztek — 5 új teszt

Fájl: `Tests/Security/RlsTenantIsolationTests.cs`

- GetDoorOrder cross-tenant → NotFound
- GetDoorOrder same-tenant → sikeres
- ListDoorOrders cross-tenant → üres lista
- ListDoorOrders same-tenant → csak saját rendelések
- Repository GetByIdAsync cross-tenant → null

## Tesztek

```
Előző: 172/172
Jelenlegi: 202/202 — Failed: 0, Skipped: 0
```

`dotnet build` → 0 error, 0 warning  
`dotnet test` → 202/202 passed

## Security review

- RLS isolation: 5 negatív teszt teszteli az alkalmazásszintű tenant szűrést ✓
- SEC-08 truncation: PDF-ben 100 char limit érvényes ✓
- OrchestratorClient: 4xx → nem tárol korrupt adatot, explicit hibát ad vissza ✓
- DoorDimensions max press size: ipari sajtó mérethatár érvényesítve ✓

## Kockázatok / kérdések

Nincs. Minden DoD pont zöld.
