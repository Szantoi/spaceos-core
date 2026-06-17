# SpaceOS Architecture Overview

> Reference fájl a `spaceos-conductor` skill számára. Olvasd be amikor új modul tervezéséhez kell kontextust adnod, vagy a felhasználó kérdezi az architektúra magas szintjét.

## Magas szintű architektúra

A SpaceOS multi-tenant B2B SaaS platform faipari (joinery/woodworking) cégeknek. Négy logikai réteg:

```
L4  Portal               (React 18 · Vite · Tailwind · TanStack Query)
                         portal.joinerytech.hu / asztalostech.hu
                              │
                              ▼ /bff/*
L3  Orchestrator         (Node.js 22 · Express · TypeScript)
                         BFF (Backend-for-Frontend) + LLM Provider abstraction
                              │
                              ▼ /api/*
L2  Modules              (.NET 8 · Minimal API)
                         Joinery, Cabinet, Cutting, Inventory, Procurement, Abstractions
                              │
                              ▼
L1  Kernel               (.NET 8 · EF Core 8 · PostgreSQL 16)
                         FlowEpic, AggregateSnapshot, ProofHash/WORM, B2B Handshake,
                         TenantHandshakeAllowlist, Audit Hash Chain
```

Plus külön kompozíció:

- **CabinetBilder.Adapter.AutoCAD** — Windows-only AutoCAD plugin, .NET 10 (AutoCAD 2027 igénye)
- **Modules.Cabinet** — platform-független domain motor a Cabinet-vertikumra (Cabinet 0.1, 0.2 sorozat)
- **FreeTier** — eszkozok.joinerytech.hu, public ingyenes eszközök (PDF generáció stb.)

## A B2B Relativity tan

**Iparági aktorok kontextus szerint változnak között gyártó és vevő.** Egy konyhabútor-gyártó **rendel** ajtót Doorstartól (vevő), ugyanakkor **eladja** a saját bútorát (gyártó). Egy szabászat **kap** anyagot (vevő) és **leszállít** kivágott panelt (gyártó).

Ezért a SpaceOS **nem külön app-okat** csinál minden szakmára, hanem **egy app-ot modulrendszerrel**:

```
tenant.EnabledModules = ['door']           → csak ajtó-workflow
tenant.EnabledModules = ['door', 'cabinet']→ ajtó + szekrény egy cégnél
tenant.EnabledModules = ['cabinet']        → csak szekrény, B2BHandshake-en rendel ajtót
```

A modulok közötti rendelés a **B2B Handshake** protokollon megy — bidirekcionális cross-tenant gateway.

## A 4-réteg vázlatos felelőssége

### L1 — Kernel
- **Domain alapok**: Tenant, FlowEpic, AggregateSnapshot, B2BHandshake, ProofHash, AuditHash chain
- **Federated data sovereignty**: Kernel csak `existence + FSM state`-et tárol, részletek a Modules-ben
- **Per-tenant hash chain**: legal/escrow evidence
- **Multi-brand support**: SourceBrand allowlist (joinerytech, asztalostech)

### L2 — Modules (üzleti vertikumok)
- **Joinery**: ajtógyártás (Phase 3 LIVE)
- **Cabinet**: szekrénygyártás (Cabinet 0.1 LIVE, Cabinet 0.2 tervezésben)
- **Cutting**: szabászat (Phase 3 LIVE)
- **Inventory**: készlet, offcut, raktár (Phase 1 LIVE)
- **Procurement**: beszerzés, beszállító, megrendelés (LIVE)
- **Abstractions**: shared port-interfészek (ICuttingProvider, IInventoryProvider, ISnapshotMigrator)

### L3 — Orchestrator
- **BFF szerep**: a Portal-tól érkező kérések proxy-zása megfelelő L2/L1 endpoint-ra
- **LLM Provider abstraction**: agentic loop, tool dispatch (Anthropic / OpenAI / Mock)
- **JWT forwarding**: Keycloak token-ek átadása a Kernel felé
- **Auth middleware**: tenant-resolution, brand detection, RLS context setting

### L4 — Portal
- **Multi-brand**: joinerytech.hu, asztalostech.hu (host-alapú brand skin)
- **Module router**: enabledModules alapján lazy-loadolja a route-okat
- **B2BHandshake panel**: cross-tenant rendelés-felület
- **Doorstar Portal**: dedikált tenant skin

## Repo-szervezés

A munkamegosztás miatt **két authoritative gép** van:

| Repo | Authoritative | Purpose |
|---|---|---|
| `spaceos-kernel` | **VPS** | L1 backend |
| `spaceos-orchestrator` | **VPS** | L3 BFF |
| `spaceos-portal` (vagy `spaceos-design-portal`) | **VPS** | L4 frontend |
| `spaceos-modules-{joinery,cabinet,cutting,...}` | **VPS** | L2 modulok |
| `spaceos-docs` | **VPS** | Globális dokumentáció (Manifesto, MFT, ADR) |
| `cabinetbilder-autocad` | **Windows** | AutoCAD plugin (csak Windows-on tesztelhető) |

**Read-only mirror-ok**: a Windows-on a spaceos-* repo-k klónja `read-only mirror`-ban él, pre-commit hook védi a véletlen commit ellen.

**NuGet feed**: GitHub Packages-en a `SpaceOS.Cabinet.*`, `SpaceOS.Modules.Contracts`, `SpaceOS.Nesting.Algorithms` csomagok kerülnek publikálásra.

## Verziózási konvenció

Cabinet sorozat **NuGet-verzió-alapú**:

| Cabinet verzió | NuGet | Scope |
|---|---|---|
| **Cabinet 0.1 — Core Foundation** | `0.1.x` | A1-A11 axiómák |
| **Cabinet 0.2 — Catalog & Assembly** | `0.2.x` | A12-A16 axiómák |
| **Cabinet 1.0 — Doorstar Soft Launch ready** | `1.0.x` | Cabinet 0.1+0.2 stabil + AutoCAD adapter |

Cutting sorozat **fázis-alapú** (a vision dokumentum szerint):

| Phase | Tartalom |
|---|---|
| Phase 1 | Contract — DTO-k, port-interfészek |
| Phase 2 | Core — domain modell, FSM |
| Phase 3 | Planning — order ingestion, nesting publish (LIVE) |
| Phase 4 | Execution — tracking, daily plan, completion proof |
| Phase 5 | Analytics — riportolás, KPI |
| Phase 6 | Adapters — OptiCut, SAP, EDI integráció |

Joinery sorozat **fázis-alapú**:

| Phase | Tartalom |
|---|---|
| Phase 1 | Initial — door order, BOM |
| Phase 2 | Production — process plan, cutting integration |
| Phase 3 | Portal UI — batch PDF, anyaglista (LIVE) |

## A 16 Cabinet axióma

A Cabinet sorozat **16 alapaxiómára** épül, amit a `SpaceOS_Cabinet_Core_Session_20260425.docx` rögzít:

| ID | Axióma | Cabinet release |
|---|---|---|
| A1 | Affin mátrix mindenhol | 0.1 |
| A2 | Két reference frame (Part / Assembly) | 0.1 |
| A3 | BaseCuboid mint gyökér | 0.1 |
| A4 | Hátfal kubus-derivált | 0.1 |
| A5 | Default joint = face-edge butt | 0.1 |
| A6 | Megmunkálás 3-féle Subject (Plane, Edge, Connection) | 0.1 |
| A7 | Szemantikus név derivált (gravity + topology) | 0.1 |
| A8 | Platform-független Core | 0.1 |
| A9 | TenantStandard mint first-class | 0.1 (port) + 0.2 (impl) |
| A10 | Szelektív újraszámítás (DependencyGraph) | 0.1 |
| A11 | Warning, sosem blokk (DesignAdvisory) | 0.1 |
| A12 | Horizontális szerep ambivalens (Shelf vs CrossRail) | 0.2 |
| A13 | Marketplace-bontás (BillOfServices ext-point) | 0.2 |
| A14 | Assembly Documentation 4. derivált nézet | 0.2 |
| A15 | Catalog mint federated, community-driven | 0.2 |
| A16 | FlowEpic skálafüggetlen (MicroAssembly scope) | 0.2 |

## A T1-T6 tenetek (Master Manifesto)

A SpaceOS irányadó értékei:

- **T1 — One source of truth** — minden adatnak egy authoritative helye van
- **T2 — Federation, not centralization** — tenant-szuverenitás
- **T3 — Adat-tulajdon** — a felhasználó adatai a felhasználóé
- **T4 — Kiszámítható biztonság** — minden security-decision-t dokumentált
- **T5 — Műszaki egyszerűség** — a sok szabály egy egyszerűbb világot szolgál
- **T6 — Felhasználó-tisztelet** — sose blokkold, mindig segíts (lásd A11)

## A munkamódszer rétege

| Eszköz | Mire | Példa |
|---|---|---|
| **claude.ai project chat** | Kreatív tervezés, review-igényes munka | v1→v4 architektúra-pipeline |
| **Claude Code agent (VPS)** | Strukturált, párhuzamosítható implementáció | Cabinet 0.1 implementáció track A-H |
| **Claude Code agent (Windows)** | AutoCAD-igényes munka | CabinetBilder integráció |
| **`spaceos-conductor` skill (jelen)** | Strategic conductor szerep | Status overview, prompt-generálás |
| **`spaceos-arch-planner` skill** | Modulszintű v1→v4 review pipeline | Cabinet 0.2 design |
| **`spaceos-session-kickoff` skill** | Új session indítás Hungarian-style | "Szia, hol tartunk?" típus |
