---
id: MSG-CABINET-003-DONE
from: cabinet
to: root
type: done
priority: high
status: READ
ref: MSG-CABINET-003
created: 2026-04-25
---

# CABINET-003 DONE -- Track C: Machining + Construction csomag

## Eredmeny

### Machining csomag (net8.0;net10.0) -- KOMPLETT

- `MachiningOperation.cs` -- 9 enum ertek (Drill, Groove, Rabbet, Pocket, Profile, EdgeBand, Cut, Chamfer, Round)
- `MachiningSubject.cs` -- abstract record + 3 concrete: PlaneSubject, EdgeSubject, ConnectionSubject (A6)
- `MachiningParameters.cs` -- optional typed params (depth, width, diameter, length, direction, placement)
- `HardwareReference.cs` -- catalog reference VO, IsValid() guard
- `MachiningFeature.cs` -- factory Create() with null/hardware validation, Result<T> pattern
- `MachiningPattern.cs` -- template pattern + GenerateFeatures() for hardware -> feature mapping

### Construction csomag (net8.0;net10.0) -- KOMPLETT

#### Engine
- `DesignAdvisory.cs` -- A11 non-blocking record
- `IConstructionContext.cs` -- TenantStandard + AssemblyDimension
- `IConstructionRule.cs` -- Apply interface + ConstructionRuleResult.Empty sentinel
- `ConstructionRuleEngine.cs` -- SEC-CAB-4 (per-rule 5s timeout, engine 30s timeout, 1000/rule output cap), A11 (exceptions -> Critical advisory), SEC-CAB-8 (null result handling)

#### 10 Default Rules
| Rule ID | Tipus | Funkcio |
|---|---|---|
| R-32mm-LineBore | Generator | 32mm raszter furas oldallapokra |
| R-Default-Joint | Validator | FaceEdgeButt default ellenorzes |
| R-BackPanel-Hidden | Generator | Groove a hatfalhoz (Groove attachment) |
| R-BackPanel-Visible | Generator | Rabbet a hatfalhoz (Rabbet attachment) |
| R-EdgeBand-FrontVisible | Generator | Elzaras front elek |
| R-EdgeBand-Hidden | Validator | Belso elek nem kapnak elzarast |
| R-Setback-15mm | Validator | 15mm setback zona |
| R-Material-Default | Advisory | Info ha part default anyagot hasznal |
| R-Stiffener-Tall | Advisory | Warning magas szekreny merevito nelkul |
| R-Shelf-Sag | Advisory | Info hosszu polc meghajlas veszelye |

### Security rules enforced

- SEC-CAB-4: Per-rule timeout (5s) + engine timeout (30s) + output cap (1000/rule)
- SEC-CAB-8: Null result handling, exception wrapping
- SEC-CAB-9: Advisory messages template-based, no tenant-specific numbers
- A11: Engine never throws, exceptions -> Critical advisory

## Definition of Done checklist

- [x] Machining csomag: MachiningFeature, MachiningSubject, HardwareReference, MachiningPattern
- [x] Construction csomag: ConstructionRuleEngine + 10 default rule
- [x] SEC-CAB-4: timeout + iteration cap
- [x] A11: Advisory non-blocking
- [x] `dotnet build -c Release` 0 error, 0 warning
- [x] `dotnet test` 268 pass (194 elozo + 74 uj) -- tulteljesiti a 259 celszamot

## Build & Test

```
Build succeeded. 0 Warning(s), 0 Error(s)
net8.0:  Passed! Failed: 0, Passed: 268, Skipped: 0
net10.0: Passed! Failed: 0, Passed: 268, Skipped: 0
```

## Teszt bontas

| Teszt fajl | Teszt szam |
|---|---|
| Geometry (korabbi) | 76 |
| Abstractions (korabbi) | 23 |
| Domain (korabbi) | 95 |
| MachiningFeatureTests | 12 |
| HardwareReferenceTests | 5 |
| MachiningPatternTests | 8 |
| MachiningOperationTests | 6 |
| DesignAdvisoryTests | 5 |
| ConstructionRuleEngineTests | 8 |
| DefaultRulesTests | 15 |
| EngineResultTests | 7 |
| Construction TestHelpers | 8 |
| **Osszesen** | **268** |
