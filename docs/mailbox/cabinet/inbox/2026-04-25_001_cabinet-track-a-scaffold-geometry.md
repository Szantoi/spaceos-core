---
id: MSG-CABINET-001
from: root
to: cabinet
type: task
priority: high
status: READ
ref: SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md
created: 2026-04-25
---

# CABINET-001 — Track A: Repo scaffold + Geometry csomag (Nap 1–3.5)

> **Tervdok:** `/opt/spaceos/docs/tasks/new/SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md` — KÖTELEZŐ olvasmány!
> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Repo:** `/opt/spaceos/spaceos-modules-cabinet/`
> **Használhatsz sub-agent-eket** ha szükséges (csharp-expert, devils-advocate)

---

## Nap 1 — Solution scaffold

### Solution + csproj-ok

```bash
dotnet new sln -n SpaceOS.Modules.Cabinet
```

7 csproj (spec §4 + §16.1):

| Csomag | Framework | Mappa |
|---|---|---|
| SpaceOS.Cabinet.Geometry | netstandard2.1 | src/SpaceOS.Cabinet.Geometry/ |
| SpaceOS.Cabinet.Abstractions | netstandard2.1 | src/SpaceOS.Cabinet.Abstractions/ |
| SpaceOS.Cabinet.Domain | net8.0;net10.0 | src/SpaceOS.Cabinet.Domain/ |
| SpaceOS.Cabinet.Machining | net8.0;net10.0 | src/SpaceOS.Cabinet.Machining/ |
| SpaceOS.Cabinet.Construction | net8.0;net10.0 | src/SpaceOS.Cabinet.Construction/ |
| SpaceOS.Cabinet.Semantics | net8.0;net10.0 | src/SpaceOS.Cabinet.Semantics/ |
| SpaceOS.Cabinet (meta) | net8.0;net10.0 | src/SpaceOS.Cabinet/ |

Test projekt:
| SpaceOS.Cabinet.Tests | net8.0;net10.0 | tests/SpaceOS.Cabinet.Tests/ |

### Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ImplicitUsings>enable</ImplicitUsings>
    <Version>0.1.0-alpha.1</Version>
    <Authors>SpaceOS</Authors>
    <Company>JoineryTech</Company>
  </PropertyGroup>
</Project>
```

### global.json

```json
{
  "sdk": {
    "version": "8.0.400",
    "rollForward": "latestFeature"
  }
}
```

**Megjegyzés:** Ha .NET 10 SDK elérhető a VPS-en, használd `10.0.x`-et. Ha nem, `8.0.x` is OK — a multi-target net10.0 a build-nél kiderül.

### .gitignore

Standard .NET gitignore (bin/, obj/, *.user, publish/).

---

## Nap 2–3.5 — Geometry csomag (spec §4.1)

### Value Objects

```csharp
// Vector3 — SEC-CAB-1: NaN/Infinity guard
public readonly struct Vector3
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    
    public static Result<Vector3> Create(double x, double y, double z)
    {
        if (double.IsNaN(x) || double.IsInfinity(x) ||
            double.IsNaN(y) || double.IsInfinity(y) ||
            double.IsNaN(z) || double.IsInfinity(z))
            return Result.Invalid("NaN/Infinity not allowed");
        return new Vector3(x, y, z);
    }
}

// AffineTransform — 4x4 mátrix (A1 axióma)
// PartFrame, AssemblyFrame — reference frame-ek (A2)
// PartDimension, AssemblyDimension — SEC-CAB-3: MaxWidthMm=50000
// GeometryConstants — BE-CAB-7: epsilon config
```

### Tesztek (50+)

- Vector3: Create valid, NaN reject, Infinity reject, equality, operations
- AffineTransform: Identity, Translation, Rotation, Compose, Inverse
- PartFrame/AssemblyFrame: Create, Transform, Convert
- Dimension: Create valid, overflow reject (>50000), zero reject
- JSON serialization round-trip (CabinetJsonOptions.Strict)

---

## Definition of Done

- [ ] Solution + 7 csproj + 1 test projekt
- [ ] Directory.Build.props + global.json
- [ ] Geometry csomag komplett: Vector3, AffineTransform, PartFrame, AssemblyFrame, Dimension
- [ ] SEC-CAB-1: NaN/Infinity guard minden geometriai factory-ban
- [ ] SEC-CAB-3: MaxDimension limit
- [ ] `dotnet build -c Release` 0 error, 0 warning
- [ ] `dotnet test` ≥ 50 pass
- [ ] Outbox DONE
