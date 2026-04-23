# SpaceOS Modules Contracts Architecture v4.2 — FreeTier Extension Points

| Mező | Érték |
|---|---|
| **Dokumentum** | `SpaceOS_Modules_Contracts_Architecture_v4_2.md` |
| **Verzió** | v4.2 |
| **Státusz** | APPROVED — 2026-04-20, Gábor elfogadta (Q-1..Q-4 döntések lezárva) |
| **Scope** | Contracts NuGet 1.2.0 → 1.3.0 + Nesting algoritmus NuGet kiszervezés |
| **Előző** | v4.1 Amendment (Reservation API — `InventoryReservation` flag, `ReserveAsync`, `ReleaseReservationAsync`, `GetReservationsAsync`) |
| **Dátum** | 2026-04-20 |
| **Szerző** | Architect terminál |
| **Fogyasztók** | Cutting modul (elsődleges), Joinery (NuGet frissítés), FreeTier.Api (v1.5, Q3 2026) |

---

## 1. Scope összefoglaló

### Mi változik

| Változás | Típusa | Törőkompatibilitás |
|---|---|---|
| `SourceChannel` enum (új) | Új publikus típus | Additív — nincs meglévő kód ami függ tőle |
| `ICuttingProvider.SubmitAnonymousSheetAsync` | Új interfész metódus | **Törőkompatibilis** — default interfész metódus (DIM) |
| `ProviderCapability.CuttingAnonymous` | Új flag érték | Additív — meglévő flag-ek változatlanok |
| `AnonymousSheetRequest` DTO | Új sealed record | Additív — nincs meglévő kód ami függ tőle |
| `SpaceOS.Nesting.Algorithms` NuGet | Új NuGet csomag | Additív — a Cutting modul továbbra is működik a régi módon |

### Mi NEM változik

- `ICuttingProvider` meglévő 6 metódusa: változatlan
- `IInventoryProvider`: változatlan (v1.2.0 marad)
- `IProcurementProvider`: változatlan
- `IModuleProvider` base: változatlan
- `ProviderCapability` meglévő 12 flagje: változatlan értékek és pozíciók
- Minden meglévő DTO, enum, event: változatlan

### Verzió bump indoklása

**MINOR bump (1.2.0 → 1.3.0)** — kizárólag új típusok és additív interfész bővítés (C# default interface method). Egyetlen meglévő típus sem változik. A SemVer 2.0 szerint ez MINOR.

---

## 2. Döntési táblázat

| # | Döntés | Opció A | Opció B | Választott | Indoklás |
|---|---|---|---|---|---|
| D-31 | `SourceChannel` helye | `Shared/` namespace | `Cutting/Enums/` | **Shared/** | FreeTier és PartnerTier a Kernel szinten is használhatja (audit event, RBAC), nem Cutting-specifikus |
| D-32 | `SubmitAnonymousSheetAsync` hozzáadása | Új metódus interfészben (breaking) | Default Interface Method (DIM) | **DIM** | Meglévő implementációk nem törnek el. A DIM `NotSupportedException`-t dob amíg nincs explicit impl. |
| D-33 | DIM default viselkedése | Dob `NotSupportedException` | Delegál `SubmitCuttingSheetAsync`-ra | **NotSupportedException** | Konzisztens a SEC-05 mintával: Consumer MUST check `CuttingAnonymous` capability. A Cutting terminál explicit implementálja ha kész. |
| D-34 | `CuttingAnonymous` flag pozíció | `1 << 12` | `1 << 13` (rövid helyet hagy) | **1 << 12** | Következő szabad pozíció. Nem szabad hézagot hagyni — a ProviderCapability [Flags] enum szekvenciális. |
| D-35 | `AnonymousSheetRequest` tartalma | Önálló flat DTO | Wrapeli `SubmitCuttingSheetRequest`-et | **Wrapper** | DRY — a cutting sheet input nem duplikált. Extra mezők: `Source`, `PartnerId`, `BrandingContextId`. |
| D-36 | Nesting algoritmus NuGet neve | `SpaceOS.Nesting` | `SpaceOS.Nesting.Algorithms` | **SpaceOS.Nesting.Algorithms** | Specifikusabb — a `SpaceOS.Nesting` név az egész nesting domain-nek fenntartva. |
| D-37 | `INestingStrategy` interface helye | Nesting NuGet | Contracts NuGet | **Nesting NuGet** | A Contracts NuGet közös modul szerződés. A nesting algoritmus implementációs részlet — nem minden modul fogyasztja. |
| D-38 | Guillotine strategy scope | v1.3.0 rész | Külön v1.4.0 epic | **v1.3.0 rész** | A Growth Strategy L2-ként hivatkozza. A Strategy Pattern bevezetése és az FFDH kiszervezése együtt jár — a Guillotine implementáció ~0.5 nap ráépítés. |
| D-39 | `MaxRectsNestingStrategy` | Teljes implementáció | Placeholder class + `NotImplementedException` | **Placeholder** | L3 algoritmus, nincs üzleti nyomás. A class létezése biztosítja a bővíthetőséget. |
| D-40 | Nesting NuGet függőség iránya | Contracts függ Nesting-től | Nesting függ Contracts-tól | **Független** | Mindkettő önálló. A Nesting NuGet saját DTO-kat használ. A Cutting modul mapperrel összeköti őket. |

---

## 3. Részletes specifikáció

### A. `SourceChannel` enum

**Fájl:** `SpaceOS.Modules.Contracts/Shared/SourceChannel.cs` (ÚJ)

```csharp
namespace SpaceOS.Modules.Contracts.Shared;

/// <summary>
/// Identifies the channel through which a request entered the system.
/// Used for audit trail, RBAC differentiation, and rate-limit policies.
/// v1.3.0 — introduced for FreeTier/PartnerTier extension points.
/// </summary>
public enum SourceChannel
{
    /// <summary>Authenticated tenant user via Portal or BFF.</summary>
    Direct = 0,

    /// <summary>Anonymous workspace (FreeTier) — no tenant context, rate-limited by IP/session.</summary>
    FreeTier = 1,

    /// <summary>B2B2C embedded flow via partner integration (PartnerTier).</summary>
    Partner = 2,

    /// <summary>Programmatic ERP/API integration (future).</summary>
    Api = 3
}
```

**Tervezési megjegyzések:**
- NEM `[Flags]` — egy request egyszerre csak egy csatornán érkezik.
- `Direct = 0` a default — meglévő kód implicit `Direct`-ként viselkedik.
- A Kernel audit event-ek a `SourceChannel`-t menthetik a `metadata` JSON mezőben (nincs schema változás, additív).

---

### B. `ICuttingProvider.SubmitAnonymousSheetAsync`

**Fájl:** `SpaceOS.Modules.Contracts/Cutting/ICuttingProvider.cs` (MÓDOSÍTVA)

Új metódus az interfész végére, Default Interface Method (DIM) implementációval:

```csharp
/// <summary>
/// Submits a cutting sheet from an anonymous or partner channel.
/// Requires <see cref="ProviderCapability.CuttingAnonymous"/> capability.
/// v1 implementation: throw NotSupportedException until CuttingAnonymous is implemented.
/// FreeTier (v1.5): delegates to SubmitCuttingSheetAsync with synthetic tenant context.
/// </summary>
/// <remarks>
/// <para><b>SEC-10 — Anonymous request policy:</b> No JWT required.
/// Rate-limited by IP (100 req/hour). Max sheet size: Lines ≤ 50 (vs 200 for authenticated).</para>
/// <para><b>SEC-11 — Partner ID validation:</b> If source is Partner,
/// partnerId MUST be a registered partner UUID. Reject unknown partners with 403.</para>
/// <para><b>Audit:</b> SourceChannel is recorded in audit event metadata.</para>
/// </remarks>
/// <param name="request">The anonymous cutting sheet submission request.</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>The new cutting sheet identifier on success.</returns>
Task<Result<Guid>> SubmitAnonymousSheetAsync(
    AnonymousSheetRequest request,
    CancellationToken ct)
{
    // DIM: providers that don't support anonymous submissions throw.
    // Consumer MUST check CuttingAnonymous capability before calling (SEC-05).
    throw new NotSupportedException(
        $"Provider does not support anonymous sheet submission. " +
        $"Check {nameof(ProviderCapability)}.{nameof(ProviderCapability.CuttingAnonymous)} before calling.");
}
```

**Implementációs szerződés a Cutting terminálnak (v1 — most):**
1. A Cutting modul `CuttingProviderHttpAdapter` NEM override-olja a DIM-et v1-ben.
2. A DIM `NotSupportedException`-t dob.
3. A `Capabilities` property NEM tartalmazza a `CuttingAnonymous` flag-et.
4. Consumer-ok (Joinery, Orchestrator) NEM hívják a metódust.

**Implementációs szerződés (v1.5 — FreeTier):**
1. A Cutting modul `CuttingProviderHttpAdapter` explicit override-olja.
2. Létrehoz egy szintetikus tenant kontextust (FreeTier temporary tenant).
3. Csatol `SourceChannel` metaadatot az audit event-hez.
4. A `Capabilities`-hez hozzáadja a `CuttingAnonymous` flag-et.

---

### C. `ProviderCapability.CuttingAnonymous` flag

**Fájl:** `SpaceOS.Modules.Contracts/Shared/ProviderCapability.cs` (MÓDOSÍTVA)

Új flag az enum végére:

```csharp
/// <summary>
/// Provider supports anonymous/unauthenticated cutting sheet submission (v1.3.0).
/// Required for FreeTier and PartnerTier flows.
/// Consumer MUST check this flag before calling SubmitAnonymousSheetAsync (SEC-05).
/// </summary>
CuttingAnonymous = 1 << 12,
```

**Biztonsági megjegyzés:** A `CuttingAnonymous` capability meglétének ellenőrzése KÖTELEZŐ a consumer oldalon mielőtt `SubmitAnonymousSheetAsync`-ot hív. Ez a meglévő SEC-05 minta. A `ProviderCapabilityTests` tesztosztályt ki kell egészíteni az új flag tesztjeivel.

---

### D. `AnonymousSheetRequest` DTO

**Fájl:** `SpaceOS.Modules.Contracts/Cutting/Requests/AnonymousSheetRequest.cs` (ÚJ)

```csharp
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Cutting.Requests;

/// <summary>
/// Request DTO for anonymous/partner cutting sheet submissions.
/// Wraps <see cref="SubmitCuttingSheetRequest"/> with channel metadata.
/// v1.3.0 — introduced for FreeTier/PartnerTier extension points.
/// </summary>
/// <remarks>
/// <para><b>SEC-10 — Anonymous constraints:</b>
/// Lines ≤ 50 (vs 200 for authenticated), CncInstructions ≤ 100 (vs 500),
/// ProcessSteps ≤ 10 (vs 50). Enforced by server-side validation.</para>
/// <para><b>SEC-11 — Partner validation:</b>
/// When Source is Partner, PartnerId MUST be a registered UUID. 403 on unknown.</para>
/// <para><b>SEC-12 — Branding context:</b>
/// BrandingContextId is a reference to the partner's visual branding config.
/// Informational only — does NOT affect nesting computation.</para>
/// </remarks>
/// <param name="Sheet">The underlying cutting sheet request (same structure as authenticated flow).</param>
/// <param name="Source">The channel through which this request arrived.</param>
/// <param name="PartnerId">Partner identifier. Required when Source is Partner, null otherwise.</param>
/// <param name="BrandingContextId">Optional reference to partner branding configuration.</param>
/// <param name="SessionFingerprint">
/// Anonymous session identifier (IP hash or browser fingerprint).
/// Used for rate-limiting and abuse detection. NOT stored in audit trail.
/// Max 128 chars. Must NOT contain PII (SEC-07).
/// </param>
public sealed record AnonymousSheetRequest(
    SubmitCuttingSheetRequest Sheet,
    SourceChannel Source,
    Guid? PartnerId,
    Guid? BrandingContextId,
    string? SessionFingerprint);
```

**Tervezési döntések:**
- `Sheet` property: nem öröklés, hanem kompozíció (wrapping). Így a `SubmitCuttingSheetRequest` nem változik.
- `SessionFingerprint`: CSAK rate-limithez. NEM PII, NEM audit. IP hash vagy browser fingerprint. A szerver eldobja miután a rate-limit checkot elvégzi.
- A `Source` mező soha nem lehet `Direct` ebben a DTO-ban — a `Direct` csatorna a meglévő `SubmitCuttingSheetAsync` metódust használja. A szerver validálja és 400-at ad ha `Source == Direct`.

---

### E. `SpaceOS.Nesting.Algorithms` NuGet csomag kiszervezés

#### E.1 Új projekt struktúra

```
spaceos-nesting-algorithms/             ← új git repo (D-40: független Contracts-tól)
  SpaceOS.Nesting.Algorithms/
    SpaceOS.Nesting.Algorithms.csproj
    INestingStrategy.cs
    NestingStrategyFactory.cs
    Models/
      NestingInput.cs
      NestingPart.cs
      AvailablePanel.cs
      NestingResult.cs
      PlacedPart.cs
      PanelAssignment.cs
    Strategies/
      FfdhNestingStrategy.cs         ← L1 — migrált a Cutting Domain-ból
      GuillotineNestingStrategy.cs   ← L2 — új implementáció
      MaxRectsNestingStrategy.cs     ← L3 — placeholder
  SpaceOS.Nesting.Algorithms.Tests/
    FfdhStrategyTests.cs
    GuillotineStrategyTests.cs
    NestingStrategyFactoryTests.cs
```

#### E.2 `INestingStrategy` interfész

```csharp
namespace SpaceOS.Nesting.Algorithms;

/// <summary>
/// Strategy interface for 2D rectangular nesting algorithms.
/// Stateless — all input via NestingInput, all output via NestingResult.
/// Thread-safe: implementations MUST be stateless (no instance fields).
/// </summary>
public interface INestingStrategy
{
    /// <summary>Human-readable name of the algorithm (e.g. "FFDH", "Guillotine").</summary>
    string AlgorithmName { get; }

    /// <summary>
    /// Computes a 2D nesting layout for the given parts on available panels.
    /// </summary>
    Task<NestingResult> ComputeAsync(NestingInput input, CancellationToken ct = default);
}
```

#### E.3 DTO-k (Nesting saját DTO-k — NEM Contracts függőség)

```csharp
namespace SpaceOS.Nesting.Algorithms.Models;

public sealed record NestingInput(
    IReadOnlyList<NestingPart> Parts,
    IReadOnlyList<AvailablePanel> Panels,
    int SawBladeGapMm = 4);

public sealed record NestingPart(
    string PartId,
    string Name,
    decimal WidthMm,
    decimal HeightMm,
    bool CanRotate = true,
    int Quantity = 1);

public sealed record AvailablePanel(
    string PanelId,
    string MaterialCode,
    decimal WidthMm,
    decimal HeightMm,
    bool IsOffcut);

public sealed record NestingResult(
    IReadOnlyList<PanelAssignment> Assignments,
    IReadOnlyList<NestingPart> UnplacedParts,
    decimal TotalWastePercentage,
    int PanelsUsed,
    string AlgorithmUsed,
    TimeSpan ComputationTime);

public sealed record PlacedPart(
    string PartId,
    string Name,
    decimal X,
    decimal Y,
    decimal WidthMm,
    decimal HeightMm,
    bool IsRotated);

public sealed record PanelAssignment(
    string PanelId,
    string MaterialCode,
    decimal PanelWidthMm,
    decimal PanelHeightMm,
    IReadOnlyList<PlacedPart> PlacedParts,
    decimal WasteAreaMm2,
    decimal UtilizationPercent);
```

#### E.4 `FfdhNestingStrategy` (L1 — migráció)

```csharp
namespace SpaceOS.Nesting.Algorithms.Strategies;

/// <summary>
/// L1 Nesting: Greedy First Fit Decreasing Height (FFDH) strip packing.
/// Offcuts have priority over full panels.
/// Migrated from SpaceOS.Modules.Cutting.Domain.Services.NestingService.
/// </summary>
public sealed class FfdhNestingStrategy : INestingStrategy
{
    public string AlgorithmName => "FFDH";

    public Task<NestingResult> ComputeAsync(NestingInput input, CancellationToken ct = default)
    {
        // Implementáció: a jelenlegi NestingService.ComputeNesting() logika
        // átírva INestingStrategy interfészre.
        //
        // Különbségek a jelenlegi implementációhoz képest:
        // 1. Quantity támogatás: NestingPart.Quantity > 1 esetén többször elhelyezi
        // 2. UnplacedParts visszaadása: ha egy part nem fér el → bekerül a listába
        // 3. ComputationTime: Stopwatch-csal méri
        // 4. Task wrapper: szinkron logika Task.FromResult-be csomagolva (CPU-bound, nem async)
        throw new NotImplementedException("Implemented by Cutting terminal in v1.3.0 sprint.");
    }
}
```

**Migráció terv:**

| Lépés | Teendő | Hatása |
|---|---|---|
| 1 | Új `spaceos-nesting-algorithms` repo létrehozása | Nincs hatása |
| 2 | `INestingStrategy` + DTO-k + `FfdhNestingStrategy` implementálása (logika portolása NestingService-ből) | Nincs hatása |
| 3 | Tesztek: jelenlegi `NestingServiceTests` logikájának átportolása + Quantity + UnplacedParts tesztek | Nincs hatása |
| 4 | Cutting modul: `SpaceOS.Nesting.Algorithms` NuGet referencia hozzáadása | Additív |
| 5 | Cutting modul: `NestingService` → `INestingStrategy` DI csere + mapping layer (~20 sor mapper) | Belső refactor, API nem változik |
| 6 | Régi `NestingService` class: `[Obsolete]` attribútum, majd 2 sprint múlva törlés | Nem-törő |

**Cutting modul adaptáció (5. lépés részletezés):**

```csharp
// DI regisztráció (Cutting modul):
services.AddSingleton<INestingStrategy, FfdhNestingStrategy>();
services.AddSingleton<NestingStrategyFactory>();

// GetNestingResultQueryHandler-ben:
// ELŐTTE: var nestingService = new NestingService();
//         var assignments = nestingService.ComputeNesting(parts, panels, sawBladeGapMm);
//
// UTÁNA:  var input = MapToNestingInput(parts, panels, sawBladeGapMm);
//         var result = await _nestingStrategy.ComputeAsync(input, ct);
//         var assignments = MapFromNestingResult(result);
```

#### E.5 `GuillotineNestingStrategy` (L2 — új)

```csharp
namespace SpaceOS.Nesting.Algorithms.Strategies;

/// <summary>
/// L2 Nesting: Guillotine cut algorithm.
/// Simulates real-world panel saw constraints — every cut goes edge-to-edge.
/// Better waste optimization than FFDH for typical woodworking patterns.
/// Target: ≥ 95% MaxCut yield (Growth Strategy benchmark).
/// </summary>
public sealed class GuillotineNestingStrategy : INestingStrategy
{
    public string AlgorithmName => "Guillotine";

    public Task<NestingResult> ComputeAsync(NestingInput input, CancellationToken ct = default)
    {
        // Guillotine algoritmus implementáció.
        // Kulcs jellemzők:
        // - Recursive partitioning: minden vágás két részre osztja a panelt
        // - Best Area Fit (BAF) heurisztika a part-panel match-eléshez
        // - Offcut-ok elsőbbséget élveznek (mint FFDH-ban)
        // - Rotáció támogatás (CanRotate)
        // Benchmark gate: ≥ 95% MaxCut yield 5 referencia cutlistán (10, 50, 100, 200, 500 db)
        throw new NotImplementedException("Implemented by Cutting terminal in v1.3.0 sprint.");
    }
}
```

#### E.6 `MaxRectsNestingStrategy` (L3 — placeholder)

```csharp
namespace SpaceOS.Nesting.Algorithms.Strategies;

/// <summary>
/// L3 Nesting: MaxRects algorithm (placeholder — not implemented in v1).
/// Future: highest-quality nesting, compute-intensive, suitable for batch processing.
/// </summary>
public sealed class MaxRectsNestingStrategy : INestingStrategy
{
    public string AlgorithmName => "MaxRects";

    public Task<NestingResult> ComputeAsync(NestingInput input, CancellationToken ct = default)
        => throw new NotImplementedException("MaxRects is a v2+ feature.");
}
```

#### E.7 `NestingStrategyFactory`

```csharp
namespace SpaceOS.Nesting.Algorithms;

/// <summary>Factory for selecting nesting strategies by algorithm name.</summary>
public sealed class NestingStrategyFactory
{
    private readonly IReadOnlyDictionary<string, INestingStrategy> _strategies;

    public NestingStrategyFactory(IEnumerable<INestingStrategy> strategies)
        => _strategies = strategies.ToDictionary(s => s.AlgorithmName, StringComparer.OrdinalIgnoreCase);

    public INestingStrategy GetStrategy(string algorithmName)
        => _strategies.TryGetValue(algorithmName, out var s)
            ? s
            : throw new KeyNotFoundException($"No nesting strategy registered for '{algorithmName}'.");

    public IEnumerable<string> AvailableStrategies => _strategies.Keys;
}
```

#### E.8 NuGet csomag konfiguráció

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <PackageId>SpaceOS.Nesting.Algorithms</PackageId>
    <Version>1.0.0</Version>
    <Authors>SpaceOS</Authors>
    <Description>2D rectangular nesting algorithms for panel cutting optimization (FFDH, Guillotine, MaxRects)</Description>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
  <!-- NINCS függőség SpaceOS.Modules.Contracts-ra — szándékosan független (D-40) -->
  <!-- NINCS függőség Ardalis.Result-ra — pure algorithm library -->
</Project>
```

---

## 4. Törő változás elemzés (Breaking Change Analysis)

| Elem | Törő? | Indoklás |
|---|---|---|
| `SourceChannel` enum | NEM | Új típus — senki nem függ tőle |
| `SubmitAnonymousSheetAsync` DIM | NEM | Default Interface Method — meglévő implementációk fordulnak módosítás nélkül |
| `CuttingAnonymous` flag | NEM | Új `[Flags]` érték — meglévő flag-ek nem változnak |
| `AnonymousSheetRequest` DTO | NEM | Új típus |
| `SpaceOS.Nesting.Algorithms` NuGet | NEM | Új csomag — a Cutting modul régi `NestingService` class-a megmarad `[Obsolete]` jelzőjével |

**Kockázatok:**
- **DIM és .NET 8 kompatibilitás:** A DIM (C# 8.0+) teljesen támogatott .NET 8-ban. A meglévő `CuttingProviderHttpAdapter` és minden teszt stub automatikusan örökli a default implementációt.
- **ProviderCapability flag értékek:** A `1 << 12` pozíció a következő szabad bit. A teszt suite (`ProviderCapabilityTests`) ellenőrzi a flag értékek egyediségét — ezt ki kell bővíteni.

**Verifikáció ellenőrző lista (Cutting terminál számára):**
- [ ] `dotnet build` sikeres a Cutting modulban Contracts 1.3.0-val
- [ ] `dotnet test` átszelesül — meglévő tesztek változatlanul átmennek
- [ ] `ProviderCapabilityTests` bővítve az új flag-gel
- [ ] `CuttingContractTests` bővítve az új metódus és DTO-k tesztjeivel

---

## 5. Fogyasztó hatáselemzés (Consumer Impact)

| Modul | Teendő | Mikor |
|---|---|---|
| **spaceos-modules-contracts** | Contracts 1.2.0 → 1.3.0 verzió bump, új fájlok hozzáadása | **Most** (v1.3.0 release) |
| **spaceos-modules-cutting** | (1) Contracts 1.3.0 NuGet frissítés. (2) Nesting NuGet referencia + mapping layer + `INestingStrategy` DI. (3) `NestingService` `[Obsolete]`. | **Most** (1 sprint) |
| **spaceos-modules-joinery** | Contracts 1.3.0 NuGet frissítés. Nincs kód változás. | **Most** (0.5 óra) |
| **spaceos-modules-inventory** | Nem érintett. | — |
| **spaceos-modules-procurement** | Nem érintett. | — |
| **spaceos-orchestrator** | Nem érintett v1.3.0-ban. FreeTier (v1.5) esetén új BFF route kell: `/bff/freetier/*` requireAuth nélkül. | **v1.5** |
| **FreeTier.Api** (új service) | Fogyasztja a `SpaceOS.Nesting.Algorithms` NuGet-et közvetlenül. NEM függ a Cutting modultól. | **v1.5 (Q3 2026)** |
| **PartnerTier** (új service) | Fogyasztja a Contracts `AnonymousSheetRequest`-et és a `SourceChannel.Partner` értéket. | **v2 (Q4 2026)** |

---

## 6. Ráfordítási becslés

| Feladat | Ki | Becslés | Megjegyzés |
|---|---|---|---|
| A. `SourceChannel` enum | Contracts terminál | 0.5 óra | 1 fájl, triviális |
| B. `ICuttingProvider` DIM bővítés | Contracts terminál | 1 óra | DIM + doc comments + SEC hivatkozások |
| C. `CuttingAnonymous` flag | Contracts terminál | 0.5 óra | 1 sor + teszt bővítés |
| D. `AnonymousSheetRequest` DTO | Contracts terminál | 1 óra | Record + doc comments |
| E1. Nesting NuGet: projekt setup + `INestingStrategy` + DTO-k | Cutting terminál | 2 óra | Új repo, csproj, interfész, 6 DTO |
| E2. Nesting NuGet: `FfdhNestingStrategy` migráció | Cutting terminál | 3 óra | Logika átírása + Quantity támogatás + tesztek |
| E3. Nesting NuGet: `GuillotineNestingStrategy` (L2) | Cutting terminál | 4 óra | Új algoritmus implementáció + tesztek |
| E4. Nesting NuGet: `MaxRectsNestingStrategy` placeholder | Cutting terminál | 0.5 óra | Class + `NotImplementedException` |
| E5. Cutting modul: Nesting NuGet integráció + mapping | Cutting terminál | 2 óra | DI csere, mapper, régi class `[Obsolete]` |
| **Összesen** | | **~1.5 nap** | |

**Javasolt sorrend:**
1. **Contracts 1.3.0** release (A+B+C+D) — ~0.5 nap (Abstractions/Contracts terminál)
2. **Nesting NuGet 1.0.0** (E1+E2+E3+E4) — ~1 nap (Cutting terminál)
3. **Cutting modul integráció** (E5) — ~0.25 nap (Cutting terminál)

---

## 7. Nyitott kérdések Gábornak

| # | Kérdés | Háttérinformáció | Javasolt válasz |
|---|---|---|---|
| Q-1 | A `FreeTier` anonymous nesting max lines limitjét 50-re állítsuk (vs 200 az authenticated flow-ban)? | A Growth Strategy nem specifikálta a limitet. 50 elég egy tipikus bútor/ajtó projekthez. | **✅ DÖNTÉS: 50** — FreeTier = demo/próba, nem production batch. |
| Q-2 | A `SessionFingerprint` IP hash vagy browser fingerprint legyen? | IP hash egyszerűbb (szerver oldali), de NAT mögötti usereknél túl agresszív. | **✅ DÖNTÉS: IP hash v1.5-ben**, browser fingerprint v2-ben. |
| Q-3 | A `SpaceOS.Nesting.Algorithms` külön git repo legyen, vagy a `spaceos-modules-cutting` repo-ban éljen? | Külön repo: tisztább függőség, önálló CI/CD. Mono-repo: egyszerűbb fejlesztés. | **✅ DÖNTÉS: Külön repo** — a FreeTier.Api nem importálhatja a teljes Cutting modult. |
| Q-4 | A `GuillotineNestingStrategy` (L2) a v1.3.0 részként készüljön el, vagy halasszuk v1.4.0-ra? | A Growth Strategy L2-ként hivatkozza. ~4 óra munka. | **✅ DÖNTÉS: v1.3.0-ban** — a Strategy Pattern bevezetésével együtt egyszerűbb. |

---

## 8. ADR hivatkozások

### Új ADR-ek szükségesek

| ADR | Cím | Tárgyalt opció | Döntéskérő |
|---|---|---|---|
| **ADR-031** | SourceChannel enum scope és helye | Shared vs Cutting namespace | D-31 |
| **ADR-032** | Default Interface Method (DIM) használata Contracts bővítéshez | DIM vs breaking interface change vs új interfész | D-32, D-33 |
| **ADR-033** | Nesting algoritmus kiszervezés önálló NuGet csomagba | Önálló csomag vs Contracts bővítés vs Cutting Domain-ban marad | D-36, D-37, D-40 |

### Meglévő ADR-ek amelyek relevánsak

| ADR | Relevancia |
|---|---|
| ADR-025 (ProviderStub vs HttpAdapter) | Az `AnonymousSheetRequest` flow-ban is ugyanez a minta — v1-ben a DIM stub-ként viselkedik |
| ADR-027 (DenyWebRequestSentinel) | FreeTier flow-ban nincs JWT → a Sentinel minta releváns lesz az anonymous tenant handling-hez |

---

## 9. Függőségi diagram

```
                    SpaceOS.Modules.Contracts v1.3.0
                    ┌────────────────────────────────┐
                    │  Shared/                       │
                    │    SourceChannel.cs  (ÚJ)      │
                    │    ProviderCapability.cs (MÓD) │
                    │  Cutting/                      │
                    │    ICuttingProvider.cs  (MÓD)  │
                    │    Requests/                   │
                    │      AnonymousSheetRequest (ÚJ)│
                    └──────────┬─────────────────────┘
                               │ NuGet ref
                ┌──────────────┼────────────┐
                ▼              ▼            ▼
           Cutting         Joinery     [FreeTier.Api]
           Module          Module       (v1.5, Q3)
                │
                │ NuGet ref
                ▼
    SpaceOS.Nesting.Algorithms v1.0.0
    ┌────────────────────────────────┐
    │  INestingStrategy              │
    │  FfdhNestingStrategy (L1)      │
    │  GuillotineNestingStrategy (L2)│  ◄── [FreeTier.Api] is NuGet ref (v1.5)
    │  MaxRectsNestingStrategy (L3)  │
    │  NestingStrategyFactory        │
    │  Models/ (saját DTO-k)         │
    └────────────────────────────────┘
    (NINCS függőség Contracts-ra! — D-40)
```

---

## 10. Teszt elvárások

### Contracts 1.3.0 tesztek (bővítés)

| Teszt fájl | Új tesztek |
|---|---|
| `ProviderCapabilityTests.cs` | `CuttingAnonymous` flag értéke `4096` (1<<12). Flag egyediség teszt újrafutás. |
| `CuttingContractTests.cs` | `AnonymousSheetRequest` record equality, null `PartnerId` amikor `Source != Partner`, DIM dob `NotSupportedException`. |
| (új) `SourceChannelTests.cs` | Enum értékek verifikációja (Direct=0, FreeTier=1, Partner=2, Api=3). |

### Nesting NuGet tesztek

| Teszt fájl | Tartalom |
|---|---|
| `FfdhStrategyTests.cs` | A jelenlegi `NestingServiceTests` logikája átportolva + Quantity támogatás tesztek + UnplacedParts tesztek. |
| `GuillotineStrategyTests.cs` | Edge-to-edge vágás verifikáció, offcut prioritás, rotáció. MaxCut benchmark gate (≥ 95% yield). |
| `NestingStrategyFactoryTests.cs` | Name lookup, unknown name `KeyNotFoundException`. |

---

*Dokumentum vége. Következő lépés: Gábor jóváhagyása → Root kiadja a Contracts és Cutting termináloknak.*
