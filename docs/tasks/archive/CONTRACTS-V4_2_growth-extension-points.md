---
id: CONTRACTS-V4_2
title: Contracts v1.3.0 — Growth Strategy Extension Points
status: active
priority: high
assignee: abstractions
epic: growth-strategy
blocked_by: none
created: 2026-04-20
updated: 2026-04-20
docs:
  - docs/architecture/SpaceOS_Modules_Contracts_Architecture_v4_2.md
  - docs/tasks/new/SpaceOS_Growth_Strategy_v1.md
---

# CONTRACTS-V4_2 — Growth Strategy Extension Points

## Scope

Contracts NuGet 1.2.0 → 1.3.0 + SpaceOS.Nesting.Algorithms NuGet 1.0.0

**Architect spec:** `docs/architecture/SpaceOS_Modules_Contracts_Architecture_v4_2.md`

## Deliverables

### Contracts 1.3.0 (Abstractions terminál — ~0.5 nap)
- `Shared/SourceChannel.cs` — új enum (Direct/FreeTier/Partner/Api)
- `Shared/ProviderCapability.cs` — +`CuttingAnonymous = 1 << 12`
- `Cutting/ICuttingProvider.cs` — +`SubmitAnonymousSheetAsync` DIM
- `Cutting/Requests/AnonymousSheetRequest.cs` — új record
- Tesztek: ProviderCapabilityTests, CuttingContractTests, SourceChannelTests

### SpaceOS.Nesting.Algorithms 1.0.0 (Cutting terminál — ~1 nap)
- Új repo: `spaceos-nesting-algorithms`
- `INestingStrategy` interfész + `NestingStrategyFactory`
- `FfdhNestingStrategy` (L1 — migráció NestingService-ből)
- `GuillotineNestingStrategy` (L2 — új, MaxCut ≥ 95% benchmark)
- `MaxRectsNestingStrategy` (L3 — placeholder)
- DTO-k: NestingInput, NestingPart, AvailablePanel, NestingResult, stb.
- Tesztek: 20+ (Ffdh + Guillotine + Factory)

### Cutting modul integráció (Cutting terminál — ~0.25 nap)
- Contracts 1.3.0 NuGet frissítés
- Nesting NuGet referencia + mapping layer
- `NestingService` → `[Obsolete]`

## Nyitott kérdések (Gábor döntés szükséges)

- Q-1: FreeTier max lines limit: **50 javasolt**
- Q-2: SessionFingerprint: **IP hash v1.5-ben**
- Q-3: Nesting NuGet repo: **külön repo javasolt**
- Q-4: GuillotineStrategy scope: **v1.3.0-ban javasolt**
