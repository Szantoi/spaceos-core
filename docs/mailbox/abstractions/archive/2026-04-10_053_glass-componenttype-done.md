---
id: MSG-ABSTRACTIONS-053-DONE
from: abstractions
to: root
type: response
status: DONE
ref: MSG-ABSTRACTIONS-053
created: 2026-04-10
---

## Összefoglaló

MSG-ABSTRACTIONS-053 teljesítve — "Glass" ComponentType hozzáadva, FAF_Ü javítva, ManufacturingDerivation bővítve.

### 1. ComponentSlot whitelist bővítése

`Domain/Entities/ComponentSlot.cs` — `AllowedComponentTypes`-ba felvéve: `"Glass"`

### 2. FafUTemplateSeeder javítása

`Infrastructure/Seeding/FafUTemplateSeeder.cs`:
- Üveg slot: `"Virtual"` → `"Glass"`, `isVirtual: true` → `isVirtual: false`

### 3. GlassOrderItem domain record

`Domain/Results/GlassOrderItem.cs` — új sealed record:
`(SlotId, SlotName, Width, Height, Quantity, OrderDescription)`

### 4. IManufacturingDerivation bővítése

`Domain/Services/IManufacturingDerivation.cs` — új metódus:
`IReadOnlyList<GlassOrderItem> DeriveGlassOrderItems(CalculationResult result)`

### 5. ManufacturingDerivationService

- `DeriveCncPlan`: Glass slotok kizárva (`slot.ComponentType == "Glass"` → skip)
- `DeriveGlassOrderItems`: Glass típusú slotok → `"Üveg megrendelés: {W}×{H} mm, {Qty} db"` OrderDescription

### 6. GraphCalculationEngine

`CuttingList` szűrő: `!s.IsVirtual && s.ComponentType != "Glass"` — Glass nem kerül CNC szabászlistába

### 7. Seeder validator teszt frissítés

`Tests/Seeding/TemplateSeederValidationTests.cs` — FAF_Ü teszt frissítve: "Virtual"→"Glass", isVirtual:true→false (konzisztens a seederrel)

## Tesztek

- Baseline: 69 teszt (mind zöld maradt)
- Új `ComponentSlotTests.cs` (2 db):
  - Glass ComponentType elfogadott → IsSuccess
  - Ismeretlen típus → elutasítva
- Új `GlassOrderItemTests.cs` (4 db):
  - Glass kizárva CuttingList-ből
  - Glass megjelenik DeriveGlassOrderItems-ben (helyes méretekkel)
  - Glass kizárva DeriveCncPlan-ból
  - Nincs Glass slot → DeriveGlassOrderItems üres lista
- Frissített `TemplateSeederValidationTests.cs` (+1 új):
  - FAF_Ü glass slot ComponentType == "Glass" és IsVirtual == false

**Végeredmény: 76/76 teszt zöld, 0 error, 0 warning**

## Security review

- **ManufacturerOnly**: nem érintett — meglévő végpontok változatlanok
- **RLS**: nem érintett
- **Injection**: Glass ComponentType ellenőrzés domain whitelist-ben — nem user input alapján terjed
- **OWASP**: nem érintett
- **Kahn's iteratív BFS**: változatlan (GraphCalculationEngine)

## Kockázatok / kérdések

Nincs. A Glass típus most szemantikailag helyes: valós fizikai komponens, gyártáslapra kerül mint külső megrendelési tétel, de nem CNC-s sor.
