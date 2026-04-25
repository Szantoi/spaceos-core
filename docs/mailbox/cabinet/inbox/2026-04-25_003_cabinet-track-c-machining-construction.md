---
id: MSG-CABINET-003
from: root
to: cabinet
type: task
priority: high
status: READ
ref: MSG-CABINET-002-DONE
created: 2026-04-25
---

# CABINET-003 — Track C: Machining + Construction csomag (Nap 8–11)

> **Tervdok:** `/opt/spaceos/docs/tasks/new/SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md` — Section 4.3, 4.4
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** CABINET-002 ✅ (194 teszt, Geometry + Abstractions + Domain)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Machining csomag (spec §4.3)

### MachiningFeature (A6)

```csharp
public sealed class MachiningFeature
{
    public Guid Id { get; }
    public MachiningSubject Subject { get; }     // Plane, Edge, Connection
    public MachiningType Type { get; }            // Drill, Route, Dado, Rabbet, Edge-band
    public Vector3 Position { get; }
    public MachiningDimension Dimension { get; }
    public HardwareReference? Hardware { get; }   // vasalat-binding
}

// MachiningSubject enum: Plane, Edge, Connection (A6: 3 subject)
// MachiningType enum: drill, route, dado, rabbet, edgeBand, pocket, throughCut
```

### HardwareReference

```csharp
public sealed record HardwareReference(
    string CatalogId,
    string Name,
    string? Sku,
    MachiningPattern Pattern    // a vasalat milyen megmunkálásokat igényel
);
```

### MachiningPattern

Egy vasalat több megmunkálást igényelhet (pl. pántfúrás = 3 drill). A `MachiningPattern` egy template ami `MachiningFeature`-öket generál.

---

## Construction csomag (spec §4.4)

### ConstructionRuleEngine

```csharp
public sealed class ConstructionRuleEngine
{
    private readonly IReadOnlyList<IConstructionRule> _rules;
    
    // SEC-CAB-4: timeout + iteration cap
    public Result<ConstructionResult> Apply(Skeleton skeleton, int maxIterations = 100)
    {
        // Szabályok alkalmazása iteratívan amíg stabil (nincs változás)
        // SEC-CAB-4: ha maxIterations elérve → Result.Error
    }
}
```

### 10 Default Rule (32mm system)

A spec §4.4.2-ben definiált 10 szabály — a tervdokot olvasd el a részletekért. Minimum:
1. DrillingPatternRule — 32mm raszter fúrás
2. HingeRule — pántfúrás elhelyezés
3. ShelfPinRule — polctartó fúrás
4. DadoRule — horony a polchoz
5. BackPanelRule — hátfal horony (A4)
6. EdgeBandRule — élzárás
7. ConnectorRule — bútorlap összekötő (Minifix, Rafix)
8. DrawerSlideRule — fiókcsúszka fúrás
9. KickboardRule — lábazat
10. TopAttachmentRule — tetőlap rögzítés

### DesignAdvisory (A11)

```csharp
public sealed record DesignAdvisory(
    AdvisorySeverity Severity,  // Info, Warning, Error
    string RuleId,
    string Message,
    Guid? AffectedPartId
);

// A11: Advisory SOSEM blokkol — a RuleEngine Result-ja mindig tartalmazza az advisories-t,
// de a Skeleton módosítás akkor is megtörténik
```

---

## Tesztek (65+)

**Machining (25+):**
- MachiningFeature: Create, Subject types, Dimension validation
- HardwareReference: Create, pattern generation
- MachiningPattern: vasalat → feature-ök mapping

**Construction (40+):**
- ConstructionRuleEngine: Apply happy path, iteration cap (SEC-CAB-4)
- Minden default rule: minimum 1 teszt rule-onként (10 teszt)
- 32mm raszter: drill positions verify
- Advisory: Warning szintek, A11 non-blocking verify
- Rule composition: 2+ rule egymás után alkalmazva

---

## Definition of Done

- [ ] Machining csomag: MachiningFeature, MachiningSubject, HardwareReference, MachiningPattern
- [ ] Construction csomag: ConstructionRuleEngine + 10 default rule
- [ ] SEC-CAB-4: timeout + iteration cap
- [ ] A11: Advisory non-blocking
- [ ] `dotnet build -c Release` 0 error, 0 warning
- [ ] `dotnet test` ≥ 259 pass (194 előző + 65 új)
- [ ] Outbox DONE
