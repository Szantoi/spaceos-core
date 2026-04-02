# SpaceOS — Project Vision & Development Master Overview

> **Version:** 2026.03.31
> **Status:** Layer 1 (Kernel) — Production Ready | Layer 2–4 — In Progress
> **Maintainer:** Gábor

---

## 1. Az Alapvízió (The Core Vision)

A **SpaceOS** egy láthatatlan, generikus **térinformatikai és FinTech operációs rendszer** az építőipar és a bútorgyártás számára. Nem egy CAD program. Nem egy projektmenedzsment tool. Egy **platform**, amelyre iparági vertikális alkalmazások (Brandek) épülnek.

Három kritikus iparági problémát old meg:

| Probléma | Megoldás |
|---|---|
| **Technológiai szakadék** | LLM + Tool Calling: laikus szöveget milliméter-pontos CNC adattá fordít |
| **Kommunikációs káosz** | Single Source of Truth (JSON State) + FSM: minden szakág, minden cég szinkronban |
| **Pénzügyi bizalmatlanság** | Immutable event log + SHA-256 hashing + Stripe Escrow: letagadhatatlan, automatikus kifizetések |

### Az OS-analógia

Ahogy a Windows elválasztja a hardvert az alkalmazásoktól, a SpaceOS elválasztja az **építőipari matematikát és pénzügyeket** a felhasználói felületektől. A látható termékek — JoineryTech, DesignPortal, ElectroPlan — csak „appok" a közös kernelen.

---

## 2. A Négy Réteg (System Layers)

```
┌─────────────────────────────────────────────────────────┐
│  LAYER 4 — USER SPACE                                   │
│  React + TypeScript + Vite (Turborepo Monorepo)         │
│  DesignPortal · JoineryTech · ElectroPlan               │
├─────────────────────────────────────────────────────────┤
│  LAYER 3 — ORCHESTRATOR / BFF                           │
│  Node.js + TypeScript + Express + Anthropic SDK         │
│  LLM Tool Calling · API Gateway · NLP → JSON            │
├─────────────────────────────────────────────────────────┤
│  LAYER 2 — DRIVERS (Szakterületi Modulok)               │
│  C# .NET 8 Class Libraries                              │
│  Modules.Joinery · Modules.MEP · Modules.Pricing        │
├─────────────────────────────────────────────────────────┤
│  LAYER 1 — KERNEL (Platform Mag)                        │
│  C# ASP.NET Core 8 · EF Core · PostgreSQL               │
│  Auth · Escrow · Audit · Hashing · FSM                  │
└─────────────────────────────────────────────────────────┘
```

### A "Island" architektúra alapelve

Az LLM **teljes mértékben decoupled** a Kernel-től. Az Orchestrator az egyetlen komponens, amely mindkét oldalt ismeri. Ezt az elvet minden döntésnél érvényesíteni kell:

- A frontend **soha** nem hív Kernel-t közvetlenül.
- Az LLM **soha** nem számol geometriát — csak paramétereket ad át.
- A Kernel **soha** nem tartalmaz LLM API hívásokat.

---

## 3. Az Adatáramlás (Data → Rules → Geometry)

Ez a projekt egyetlen legfontosabb axiómája. **Megszegése blocker.**

```
[USER / LLM]          [C# DRIVERS]          [FRONTEND]
Intent + Params  →→→  Business Rules  →→→   Dumb Rendering
(W, H, D)             (CutList, HW)         (SVG rect-ek)

Frontend SOHA nem von le 18mm-t. Ez a C# dolga.
```

---

## 4. Jelenlegi Állapot (Current State — 2026.03.31)

### Layer 1 — C# Kernel ✅ PRODUCTION READY

| Metrika | Érték |
|---|---|
| Build | 0 hiba, 0 figyelmeztetés |
| Tesztek | **350 passing**, 0 failed |
| Projektek | 7 (Domain, Application, Infrastructure, Api, Tests, IntegrationTests, Api.Tests) |
| Adatbázis | PostgreSQL + EF Core 8, 3 migration |
| Auth | JWT Bearer, HS256, RBAC (Joiner / Designer / Admin) |
| Rate Limiting | Fixed (100 req/60s GET) + Sliding (20 req/60s POST/PUT) |
| Docker | Multi-stage Alpine, 182MB, non-root user, healthcheck |
| OpenAPI | Swagger UI, JWT Bearer, teljes endpoint dokumentáció |
| Audit Log | SHA-256 hash, append-only, per-tenant szűrhető, lapozható |
| Epics státusza | **E1–E10 CLOSED_DONE** (E5 .NET 10 migráció: 2026. november után) |

**5 Aggregate Root:** `Tenant` · `Facility` · `WorkStation` · `SpaceLayer` · `FlowEpic`

**Kész domain képességek:**
- Multi-tenant alapinfrastruktúra
- Federated SpaceLayer (lokális JSON + külső URL + ExternalAuthToken)
- FlowEpic FSM + B2B Handshake (más Tenant-ra delegálás)
- Immutable AuditEvent minden domain eventre
- Paginated, filterable lista queryek Ardalis.Specification-ön keresztül

### Layer 3 — Node.js Orchestrator (spaceos-orchestrator) 🟡 DEPLOYED, EARLY STAGE

| Metrika | Érték |
|---|---|
| Helye | VPS: `/opt/spaceos/spaceos.orchestrator` |
| Állapot | Starter csomag deployolva, fut |
| LLM | Anthropic SDK + `ILlmProvider` absztrakció (provider-agnosztikus) |
| Fontos | **Stateless** — nincs adatbázis, soha nem lesz |
| Elvégzett fix | `dotenv` csomag, Anthropic SDK típuscast |
| Hiányos | Tool registry, valós Kernel integráció, éles tool definíciók |

### Layer 4 — React DesignPortal (spaceos-design-portal) 🟡 SCAFFOLDED

| Metrika | Érték |
|---|---|
| Stack | React 18, TypeScript, Vite, TanStack Query, Zustand |
| Kész | Starter struktúra, API services, auth store, shared komponensek (FsmBadge, PagedTable) |
| Epics | E18–E27 fájlok létrehozva |
| Hiányos | Security review (REVIEW_CHECKLIST.md: A1, S1, C4, R1, T1, G5), REVIEW_REPORT.md fájlok |

### Layer 2 — C# Drivers 🔴 NOT STARTED

`Modules.Joinery` — a faipari üzleti logika (ráhagyások, élzárás, vasalat pozíciók) nincs elkezdve.

---

## 5. A Domain Modell (Bounded Contexts)

### 5.1 Identity & Federation Context

```
Tenant (jogi személy, cég)
  └── Facility (telephely, gyárcsarnok)
        └── WorkStation (CNC gép, szerelőasztal)
        └── SpaceLayer (iparági réteg — lokális vagy federált)
        └── FlowEpic (munkafolyamat atom)
```

### 5.2 Flow & Logistics Context (FSM)

```
FlowProgram → FlowProject → FlowMilestone → FlowEpic → FlowTask
                                                ↑
                                    Ez a "Delegation Boundary"
                                    A Kernel csak Epic szintig lát.
                                    A Taskok a szakági modulokban élnek.
```

**FSM State gép:**
```
BACKLOG_READY → IN_DEV → CODE_REVIEW → QA_WAITING → QA_IN_PROGRESS
→ ARCHITECT_SIGNOFF → [WAITING_FOR_INPUT ← JIT BLOCKED]
→ CLOSED_DONE | CLOSED_BLOCKED | ESCALATED
```

### 5.3 B2B Relativity (Dimenzióugrás)

```
Generálkivitelező (Host):
  FlowEpic: "Konyhabútor gyártása" → AssigneeTenantId: Asztalos Kft.

Asztalos Kft. (Guest):
  FlowProject: "Kovácsék Konyhája" (OriginExternalEpicId → GC Epic-je)
    └── TradeEpic: Alsószekrények
         └── FlowTask: Lapszabászat
         └── FlowTask: Élzárás
```

Amikor az Asztalos utolsó taskja `CLOSED_DONE` → Mediator event → GC Epic-je automatikusan `CLOSED_DONE` → JIT trigger a Villanyszerelőnek.

### 5.4 Spatial & Physics Context

A gravitációs vektor (`g = (0, -1, 0)`) mint nulladik axióma. Az alkatrészek `normálvektor · g` alapján kerülnek besorolásra:

| Halmaz | Feltétel | Példa |
|---|---|---|
| Függőlegesek | `n · g = 0` | Oldallapok, Hátfalak, Frontok |
| Vízszintesek | `n · g = ±1` | Fenéklap, Tetőlap, Polcok |
| Ferdék | egyéb | Tetőtéri vágások, rámpák |

### 5.5 Trust & Immutability Context

```
Minden domain event:
  → JSON szerializálás
  → SHA-256 hash generálás
  → AuditEvent (append-only) mentés
  → Stripe Escrow trigger (Milestone szinten)
```

---

## 6. A Multi-Brand Architektúra (Hub & Spokes)

```
                    ┌──────────────────┐
                    │   SpaceOS KERNEL  │  ← Láthatatlan platform
                    │  (C# + PostgreSQL)│
                    └────────┬─────────┘
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
    ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
    │ DesignPortal │ │ JoineryTech  │ │  ElectroPlan │
    │  (Megrendelő)│ │  (Asztalos)  │ │ (Villany/Gép)│
    │  Prémium UI  │ │  Mérnöki UI  │ │  Technikai UI│
    └──────────────┘ └──────────────┘ └──────────────┘
```

Minden Brand saját BFF példányt kap az Orchestrator rétegben, saját **System Prompt**-tal és saját **Tool Registry**-vel. A Kernel kód 100% közös.

---

## 7. A Federated Network Modell

```
GC szervere (Hub)                 Asztalos szervere (Node)
────────────────                  ────────────────────────
SpaceLayer (Architecture)         SpaceLayer (Joinery)
  IsExternalNode: false             IsExternalNode: false
                                    ExternalSourceUrl: null
                    ↕ B2BHandshake (M2M OAuth2 / SIP Protocol)
                    ↕ Webhook push (LOD 200 JSON, pár KB)
                    ↕ GC csak Bounding Boxot kap (titokvédelem)
```

**3 adatizolációs szint:**
1. **Shared DB** — kis cégek, Row-Level Security TenantId alapján
2. **Database-per-Tenant** — közepes üzemek, dynamic ConnectionString JWT alapján
3. **Server-per-Trade / On-Premise** — Enterprise, Docker + Intranet, offline működéssel

---

## 8. A JoineryTech MVP (Layer 2 következő lépés)

A `Modules.Joinery` driver célja: a Kernel `IParametricProduct` interfészét implementálva, determinisztikus faipari szabályokat alkalmazni.

**MVP scope — egyetlen korpusztípus (Sima 2-ajtós alsószekrény):**

```
POST /api/cabinet/calculate
Body: { "width": 800, "height": 720, "depth": 510 }

Response:
  CutList: [ { name: "Bottom", length: 764, width: 474, thickness: 18 }, ... ]
  BoundingBoxes: [ { x, y, width, height }, ... ]  ← csak a frontendnek
```

**Siker kritériumok:**
- A frontend **egyetlen koordinátát sem számol** — minden a C# backendből jön
- `MaterialThickness = 18mm` nem hardkódolt — `GlobalSettings` objektumból érkezik
- `PartType` enum típusbiztonság (`Side`, `Bottom`, `Back`, `Top`)
- LLM Tool schema kész: `calculate_cabinet(width, height, depth)`

---

## 9. A Fejlesztési Roadmap

### Horizon 1 — Foundation Complete ✅ (2026 Q1)

- [x] Kernel (Layer 1) — Clean Architecture, DDD, CQRS, JWT, Rate Limiting, Docker, Audit Log, 350 teszt
- [x] Orchestrator (Layer 3) — deployed, fut
- [x] DesignPortal (Layer 4) — scaffolded

### Horizon 2 — Core Product (2026 Q2–Q3)

- [ ] **Layer 4: DesignPortal** — Security review lezárása (A1, S1, C4, R1, T1, G5), E18–E27 epics implementálása
- [ ] **Layer 3: Orchestrator** — Tool registry kiépítése, valós Kernel integráció, éles tool definíciók
- [ ] **Layer 2: Modules.Joinery** — MVP: 2-ajtós alsószekrény teljes életciklusa (CutList + BoundingBox + LLM Tool)
- [ ] **C# Agent scope fix** — csharp-expert és társai csak a Kernel `.claude/` mappájában

### Horizon 3 — Scaling (2026 Q4)

- [ ] Modules.Joinery bővítés: élzárás, vasalat logikák (Blum / Hettich), fiókdobozok
- [ ] Modules.Pricing: anyag m² alapú bekerülési kalkulátor
- [ ] Multi-brand: JoineryTech UI (mérnöki, sötét mód, CNC export)
- [ ] B2B Handshake éles implementáció (M2M OAuth2, Webhook sync)
- [ ] .NET 10 migráció (E5) — 2026. november GA után

### Horizon 4 — Enterprise (2027)

- [ ] Per-tenant adatbázis (Database-per-Tenant, dynamic ConnectionString)
- [ ] On-Premise Docker Compose csomag + SIP Protocol
- [ ] AutoCAD plugin (DXF/STEP CNC export)
- [ ] Excel kliens (Power Query / Office Scripts tömeges paraméterezés)
- [ ] Redis-backed rate limiting (horizontális skálázáshoz)
- [ ] Kubernetes / Helm manifesztek

### Horizon 5 — Web3 / DLT (2028+)

- [ ] StateHash-ek anchoring publikus blokkláncra (Polygon / Ethereum L2)
- [ ] Smart Contract integráció okos escrow feltételekhez
- [ ] SpaceOS nyílt protokoll (SIP) — harmadik fél Python / Go kliensek

---

## 10. Az 5 Golden Rule (Fejlesztési Elvek)

Minden sor kódnak meg kell felelnie ezeknek. Megszegés → blocker.

| # | Szabály | Mit jelent a gyakorlatban |
|---|---|---|
| **1** | **Data → Rules → Geometry** | Frontend csak rajzol. A C# Driver számol. Az LLM csak paramétereket ad. |
| **2** | **Modular Monolith** | Kernel nem tudja mi az asztalos. `IParametricProduct` interfészen keresztül dolgozik. |
| **3** | **Immutability & Trust** | Nincs UPDATE a CAD adatokon. Minden módosítás új verzió + SHA-256 hash. |
| **4** | **Need-To-Know RBAC** | A megrendelő nem kapja meg a gyártó belső anyaglistáját. |
| **5** | **Walking Skeleton First** | Mindig az E2E pipeline-t építjük meg előbb dummy adattal, matematikát utóbb mélyítünk. |

---

## 11. Fejlesztési Döntések (Pending / Resolved)

| Döntés | Státusz | Megjegyzés |
|---|---|---|
| Monorepo tooling: Turborepo vs Nx vs plain Vite | ⏳ Pending | Architectural Blueprint: Turborepo/Nx javasolt |
| Primary LLM provider | ✅ Resolved | Anthropic SDK, `ILlmProvider` absztrakció provider-agnosztikus |
| Stripe Escrow az MVP-ben vagy később | ⏳ Pending | Kernel infrastruktúra kész (FlowMilestone triggers), UI integráció jövő |
| DesignPortal security review retroaktív kezelése | ⏳ Pending | REVIEW_CHECKLIST.md: A1, S1, C4, R1, T1, G5 |

---

## 12. A Projekt Fájlstruktúra Elvei

```
SpaceOS.Kernel/
├── .claude/                    ← CSAK a Kernel számára (csharp-expert, stb.)
├── SpaceOS.Kernel.Domain/
├── SpaceOS.Kernel.Application/
├── SpaceOS.Infrastructure/
├── SpaceOS.Kernel.Api/
└── SpaceOS.Kernel.Tests/

spaceos-orchestrator/           ← Node.js BFF, STATELESS, nincs DB
├── .claude/                    ← Orchestrator-specifikus agentek
└── src/

spaceos-design-portal/          ← React frontend
├── .claude/                    ← Frontend-specifikus agentek
└── src/
    ├── features/               ← E18–E27 epic fájlok
    └── shared/                 ← FsmBadge, PagedTable, stb.
```

**Scope discipline:** C# tudás (csharp-expert agent, csharp-async skill, ef-core skill) kizárólag a `SpaceOS.Kernel/.claude/` mappában él. A Node.js Orchestrator projektbe ezek nem kerülnek.

---

## 13. Referencia: Kulcs DTO / API Contracts

### Kernel API végpontok (Layer 1 → Layer 3)

```
GET  /api/tenants
POST /api/tenants
GET  /api/facilities/{id}
GET  /api/flow-epics/{id}
POST /api/flow-epics/{id}/delegate
POST /api/flow-epics/{id}/start
GET  /api/audit-events?tenantId=&from=&to=&page=&pageSize=
GET  /healthz
POST /api/auth/token
```

### Orchestrator API (Layer 3 → Layer 4)

```
POST /chat          ← LLM Tool Calling belépési pont
POST /calculate     ← Parametrikus számítás átjáró (→ Kernel)
GET  /health
```

### Tervezett Joinery Tool (Layer 3 LLM Tool Schema)

```json
{
  "name": "calculate_cabinet",
  "description": "Calculates the cut list and bounding boxes for a base cabinet.",
  "parameters": {
    "type": "object",
    "properties": {
      "width":  { "type": "number", "description": "Total width in mm" },
      "height": { "type": "number", "description": "Total height in mm" },
      "depth":  { "type": "number", "description": "Total depth in mm" }
    },
    "required": ["width", "height", "depth"]
  }
}
```

---

## 14. Gyors Döntési Fa (Quick Reference)

```
Hova kerül az X logika?
│
├── Koordináta / mérés / vonás kalkuláció?
│     → Layer 2: C# Driver (Modules.Joinery stb.)
│
├── Adatbázis, auth, audit, escrow?
│     → Layer 1: C# Kernel
│
├── NLP → JSON fordítás, LLM hívás, prompt engineering?
│     → Layer 3: Node.js Orchestrator
│
├── Vizualizáció, UX, SVG rajzolás, drag & drop?
│     → Layer 4: React DesignPortal / JoineryTech
│
└── Üzleti szabály ("X vasalat Y-nál közelebb nem lehet")?
      → Layer 2: C# Driver — soha nem Layer 3 vagy 4
```

---

*Ez a dokumentum a SpaceOS fejlesztésének élő koordinációs pontja. Minden új session előtt frissíteni kell a „Jelenlegi Állapot" és a „Pending Döntések" szekciót.*
