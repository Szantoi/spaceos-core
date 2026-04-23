---
id: MSG-ABSTRACTIONS-010
from: root
to: abstractions
type: task
priority: high
status: READ
ref: SpaceOS_Modules_Contracts_Architecture_v4_2.md
created: 2026-04-20
---

# ABSTRACTIONS-010 — Contracts v1.3.0: Growth Strategy Extension Points

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Arch spec:** `docs/architecture/SpaceOS_Modules_Contracts_Architecture_v4_2.md` (APPROVED, minden döntés lezárva)
> **Timeline:** ~0.5 nap (4 fájl + tesztek)
> **Előző:** Contracts v1.2.0 (Reservation API, ABSTRACTIONS-008 DONE)

---

## Kontextus

A Growth Strategy (FreeTier v1.5, PartnerTier v2) extension point-okat igényel a Contracts csomagban. **Most kell beépíteni v1-ben**, hogy v1.5 és v2 refactor nélkül ráfeküdjön.

**MINOR bump: 1.2.0 → 1.3.0** — kizárólag additív változások, semmi törő.

---

## Feladatok (teljes spec: arch doc szekció 3/A–D)

### A. `SourceChannel` enum — ÚJ FÁJL

**Fájl:** `SpaceOS.Modules.Contracts/Shared/SourceChannel.cs`

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

---

### B. `AnonymousSheetRequest` DTO — ÚJ FÁJL

**Fájl:** `SpaceOS.Modules.Contracts/Cutting/Requests/AnonymousSheetRequest.cs`

```csharp
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Cutting.Requests;

/// <summary>
/// Request DTO for anonymous/partner cutting sheet submissions.
/// Wraps <see cref="SubmitCuttingSheetRequest"/> with channel metadata.
/// </summary>
/// <remarks>
/// <para><b>SEC-10:</b> Lines ≤ 50 (vs 200 for authenticated). Server-side enforced.</para>
/// <para><b>SEC-11:</b> When Source is Partner, PartnerId MUST be a registered UUID. 403 on unknown.</para>
/// </remarks>
/// <param name="Sheet">The cutting sheet input (same structure as authenticated flow).</param>
/// <param name="Source">The channel. MUST NOT be Direct (use SubmitCuttingSheetAsync for Direct).</param>
/// <param name="PartnerId">Required when Source is Partner, null otherwise.</param>
/// <param name="BrandingContextId">Optional partner branding config reference.</param>
/// <param name="SessionFingerprint">
/// IP hash for rate-limiting. NOT stored in audit trail. Max 128 chars. No PII (SEC-07).
/// </param>
public sealed record AnonymousSheetRequest(
    SubmitCuttingSheetRequest Sheet,
    SourceChannel Source,
    Guid? PartnerId,
    Guid? BrandingContextId,
    string? SessionFingerprint);
```

---

### C. `ProviderCapability.CuttingAnonymous` — MÓDOSÍTÁS

**Fájl:** `SpaceOS.Modules.Contracts/Shared/ProviderCapability.cs`

Az enum végére add hozzá:

```csharp
/// <summary>
/// Provider supports anonymous/unauthenticated cutting sheet submission (v1.3.0).
/// Required for FreeTier and PartnerTier flows.
/// Consumer MUST check this flag before calling SubmitAnonymousSheetAsync (SEC-05).
/// </summary>
CuttingAnonymous = 1 << 12,
```

---

### D. `ICuttingProvider.SubmitAnonymousSheetAsync` — MÓDOSÍTÁS

**Fájl:** `SpaceOS.Modules.Contracts/Cutting/ICuttingProvider.cs`

Az interfész végére add hozzá (Default Interface Method):

```csharp
/// <summary>
/// Submits a cutting sheet from an anonymous or partner channel.
/// Requires <see cref="ProviderCapability.CuttingAnonymous"/> capability (SEC-05).
/// </summary>
/// <remarks>
/// v1: DIM throws NotSupportedException — providers opt-in by overriding.
/// v1.5 (FreeTier): CuttingProviderHttpAdapter overrides with synthetic tenant context.
/// </remarks>
Task<Result<Guid>> SubmitAnonymousSheetAsync(
    AnonymousSheetRequest request,
    CancellationToken ct)
{
    throw new NotSupportedException(
        $"Provider does not support anonymous sheet submission. " +
        $"Check {nameof(ProviderCapability)}.{nameof(ProviderCapability.CuttingAnonymous)} before calling.");
}
```

---

### E. Verzió bump

**Fájl:** `SpaceOS.Modules.Contracts.csproj`

```xml
<Version>1.3.0</Version>
```

---

## Tesztek (kötelező)

| Teszt fájl | Tartalom |
|---|---|
| (új) `SourceChannelTests.cs` | Enum értékek: Direct=0, FreeTier=1, Partner=2, Api=3 |
| `ProviderCapabilityTests.cs` | +`CuttingAnonymous` értéke `4096` (1<<12), flag egyediség teszt |
| `CuttingContractTests.cs` | +`AnonymousSheetRequest` record equality, DIM dob `NotSupportedException` |

**Elvárt teszt szám:** meglévő tesztek mind zölden + min. 5 új teszt.

---

## Definition of Done

- [ ] 4 fájl módosítva/létrehozva
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (régi + új tesztek)
- [ ] `Version` bump: 1.2.0 → 1.3.0
- [ ] Outbox DONE üzenet küldve

## Megjegyzés

A spec teljes részletei: `docs/architecture/SpaceOS_Modules_Contracts_Architecture_v4_2.md` — különösen szekció 3/A–D és a biztonsági megjegyzések (SEC-05, SEC-07, SEC-10, SEC-11).
