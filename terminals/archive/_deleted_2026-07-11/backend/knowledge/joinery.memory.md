# Joinery Domain Memory

> Automatikusan betöltődik ha a feladat Joinery/Asztalos modulhoz kapcsolódik.

## Domain Scope

- **Modul:** `spaceos-modules-joinery`
- **Felelősség:** Ajtó/ablak konfiguráció, parametrikus tervezés, gyártási lapok
- **Tech stack:** .NET 8, PostgreSQL, PDF generálás
- **Első ügyfél:** Doorstar Kft. (2026 Q2 Soft Launch)

## Aktív Patterns

### 1. Parametric Product
```csharp
public interface IParametricProduct
{
    Guid Id { get; }
    string ProductType { get; }
    IDictionary<string, object> Parameters { get; }
    ValidationResult Validate();
    decimal CalculatePrice(IPricingStrategy strategy);
}
```

### 2. Door Configuration
```csharp
public class DoorConfiguration : IParametricProduct
{
    public DoorType Type { get; set; }  // Single, Double, Sliding
    public decimal Width { get; set; }   // mm
    public decimal Height { get; set; }  // mm
    public string Material { get; set; } // Oak, Pine, MDF
    public string Finish { get; set; }   // Natural, Painted, Lacquered
    public List<Accessory> Accessories { get; set; }

    public ValidationResult Validate()
    {
        // Width: 600-1200mm, Height: 1800-2400mm
        // Material-Finish compatibility check
    }
}
```

### 3. Manufacturing Sheet (Gyártási Lap)
```csharp
public class ManufacturingSheet
{
    public Guid OrderId { get; set; }
    public List<CutListItem> CutList { get; set; }
    public List<AssemblyStep> AssemblySteps { get; set; }
    public List<QualityCheckpoint> QualityChecks { get; set; }
    public byte[] GeneratePdf();
}
```

## API Endpoints

| Endpoint | Method | Auth |
|----------|--------|------|
| `/api/joinery/products` | GET | JWT |
| `/api/joinery/configure` | POST | JWT |
| `/api/joinery/validate` | POST | JWT |
| `/api/joinery/price` | POST | JWT |
| `/api/joinery/orders/{id}/manufacturing-sheet` | GET | JWT |

## Doorstar Specifics

- **Ajtótípusok:** Beltéri sima, Beltéri üveges, Tűzgátló
- **Anyagok:** Tölgy, Bükk, MDF festett
- **Felületek:** Natúr olaj, Színtelen lakk, RAL festés
- **Vasalatok:** Saját készlet + partner beszállítók

## Legutóbbi Tanulságok

- **Paraméter validáció** frontend és backend oldalon is
- **PDF generálás** QuestPDF library-vel
- **Árszámítás** anyag + munka + felár komponensekből

## Kapcsolódó Fájlok

- `src/SpaceOS.Modules.Joinery/` - Domain
- `src/SpaceOS.Modules.Joinery.Application/` - CQRS
- `src/SpaceOS.Modules.Joinery.Api/` - Endpoints
- `docs/joinery/` - Doorstar dokumentáció
