---
id: MSG-ABSTRACTIONS-053
from: root
to: abstractions
type: task
priority: high
status: READ
created: 2026-04-10
---

# MSG-ABSTRACTIONS-053: Glass ComponentType hozzáadása

## Háttér

A FAF_Ü (fa ajtófélfa, üveges) template üveg-slotja jelenleg `ComponentType: "Virtual"`-t használ, mert `"Glass"` nem szerepel a domain whitelist-en. Ez szemantikailag helytelen: az üveg valós fizikai komponens, amelynek méretei felkerülnek a gyártáslapra (de nem CNC-s, hanem külső megrendelés).

## Feladatok

### 1. ComponentSlot whitelist bővítése

`Domain/Entities/ComponentSlot.cs` — `AllowedComponentTypes` HashSet-be add hozzá: `"Glass"`

### 2. FafUTemplateSeeder javítása

`Infrastructure/Seeding/FafUTemplateSeeder.cs` — az üveg-slot:
```csharp
// előtte:
var uvegSlot = template.AddSlot("Üveg", "Virtual", null, 6m, 1, isVirtual: true, ...)
// utána:
var uvegSlot = template.AddSlot("Üveg", "Glass", null, 6m, 1, isVirtual: false, ...)
```

`isVirtual: false` — mert az üveg valós fizikai komponens, nem csak geometriai csomópont.

### 3. ManufacturingDerivationService — Glass kezelés

`Infrastructure/Services/ManufacturingDerivationService.cs`

A Glass típusú slotok:
- **NEM** kerülnek a CNC szabászlistába (nem faragja a gép)
- **IGEN** kerülnek a gyártáslapra, külön `OrderItem` sorként: `"Üveg megrendelés: {Width}×{Height} mm, {Quantity} db"`
- A derivation service különböztesse meg: `if (slot.ComponentType == "Glass") → OrderItem` vs normál `CuttingListItem`

### 4. Tesztek

- `ComponentSlotTests`: `Glass` típus elfogadott → `Result.IsSuccess`
- `FafUSeederValidationTests`: az üveg-slot `ComponentType == "Glass"` és `IsVirtual == false`
- `ManufacturingDerivationTests`: Glass slot → `OrderItem` listában jelenik meg, nem `CuttingListItem`-ként
- Meglévő 69 teszt marad zöld

## DoD

- [ ] `"Glass"` szerepel az `AllowedComponentTypes`-ban
- [ ] FAF_Ü üveg-slot `ComponentType: "Glass"`, `IsVirtual: false`
- [ ] `ManufacturingDerivationService` Glass → `OrderItem` (nem CNC-s sor)
- [ ] Új tesztek zöldek, 69 baseline zöld
- [ ] `dotnet build` 0 error, 0 warning

## Válasz

`docs/mailbox/abstractions/outbox/2026-04-10_053_glass-componenttype-done.md`
