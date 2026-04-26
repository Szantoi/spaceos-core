---
id: MSG-CABINET-006
from: root
to: cabinet
type: task
priority: high
status: READ
ref: MSG-CABINET-005-DONE
created: 2026-04-26
---

# CABINET-006 — Track C: Assembly + Domain/Semantics bővítés (Nap 7.5–11.75)

> **Tervdok:** `/opt/spaceos/docs/tasks/active/SpaceOS_Cabinet_0.2_CatalogAssembly_Architecture_v4.md` — Section 7, 5, 6
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** CABINET-005 ✅ (353 teszt, Catalog komplett)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Assembly csomag (spec §7)

### AssemblyStep entity

```csharp
public sealed class AssemblyStep
{
    public int Order { get; }
    public string Title { get; }
    public string SanitizedInstruction { get; }  // A11: markdown sanitized
    public Guid PrimaryPartId { get; }
    public IReadOnlyList<Guid> RequiredConnectionIds { get; }
    public HardwareReference? Hardware { get; }
    public IReadOnlyList<string> RequiredTools { get; }
    public TimeSpan EstimatedDuration { get; }
    public string? RequiredSkillLevel { get; }
    
    public static Result<AssemblyStep> Create(..., IMarkdownSanitizer sanitizer) { }
}
```

### ExplodedView + HardwareCallout

```csharp
public sealed record ExplodedView(
    IReadOnlyList<ExplodedViewLayer> Layers  // Part-ok rétegezve a szétszedés sorrendjében
);

public sealed record HardwareCallout(
    HardwareReference Hardware,
    Guid TargetPartId,
    Vector3 Position,
    string CalloutLabel
);
```

### AssemblyDocumentationService (A14)

- Input: Skeleton + CatalogResolutionProvider
- Output: IReadOnlyList<AssemblyStep> (rendezett)
- Algoritmus: O(N+E) topológiai sorrend, gravitáció-alapú
- BE-CAB02-8: explicit complexity dokumentálva

### IMarkdownSanitizer (SEC-CAB02-3)

- Whitelist: headers, bold, italic, lists, code blocks
- Reject: HTML tags, links, images, scripts
- Input: raw instruction markdown
- Output: sanitized markdown

---

## Domain 0.2.0 bump (spec §5)

### Skeleton aggregate bővítés

```csharp
// Új metódusok:
public Result PinCatalogEntry(Guid partId, CatalogType type, Guid catalogEntryId) { }
public Result DeriveAssembly(ICatalogResolutionProvider resolver) { }
public Result<BillOfServices> DeriveBillOfServices() { }  // A13 extension point
```

### SkeletonSnapshot 0.2

- SchemaVersion "0.2"
- Új mezők: `roleAssignments`, `pinnedCatalogEntries`
- Export sanitization: PII-mentes export

---

## Semantics 0.2.0 bump (spec §6)

### Catalog-aware InferAll

```csharp
// Új overload:
public Result<IReadOnlyDictionary<Guid, PartRole>> InferAll(
    Skeleton skeleton,
    ICatalogResolutionProvider? catalogResolver = null  // A12: catalog default réteg
) { }
```

---

## Tesztek (80+)

**Assembly (40+):**
- AssemblyStep: Create, sanitized instruction, RequiredTools
- ExplodedView: layer ordering
- HardwareCallout: position validation
- AssemblyDocumentationService: full flow, ordering, edge cases
- IMarkdownSanitizer: whitelist pass, HTML reject, script reject

**Domain bump (25+):**
- PinCatalogEntry: valid, cross-tenant reject (SEC-CAB02-2)
- DeriveAssembly: with catalog resolver
- SkeletonSnapshot 0.2: round-trip, roleAssignments, pinnedCatalogEntries
- Export sanitization

**Semantics bump (15+):**
- InferAll with catalog resolver
- Catalog default override behavior

---

## Definition of Done

- [ ] Assembly csomag: AssemblyStep, ExplodedView, HardwareCallout, AssemblyDocumentationService
- [ ] IMarkdownSanitizer (SEC-CAB02-3)
- [ ] Domain 0.2.0: PinCatalogEntry, DeriveAssembly, DeriveBillOfServices
- [ ] SkeletonSnapshot 0.2 (roleAssignments, pinnedCatalogEntries)
- [ ] Semantics 0.2.0: catalog-aware InferAll
- [ ] `dotnet build -c Release` 0 error, 0 warning
- [ ] `dotnet test` ≥ 433 pass (353 előző + 80 új)
- [ ] net8.0 ÉS net10.0 PASS
- [ ] Outbox DONE
