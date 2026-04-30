# SpaceOS — Kódbázis összesített állapotleírás

**Utolsó frissítés:** 2026-04-30 — FE referencia oldalak · 229 teszt · 0 build hiba · commit 720e106
**Környezet:** VPS prod (109.122.222.198)
**Archívum:** [`docs/codebase-history/`](codebase-history/)

---

## ⚠️ 2026-04-30 Átalakítás

### Mappa restructure

```
/opt/spaceos/
  backend/                          ← MINDEN backend ide került
    spaceos-kernel/         (5000)
    spaceos-orchestrator/   (3000)  ← AI gateway (LLM only, NEM proxy)
    spaceos-modules-joinery/        (5002)
    spaceos-modules-abstractions/   (5003)
    spaceos-modules-inventory/      (5004)
    spaceos-modules-cutting/        (5005)
    spaceos-modules-procurement/    (5006)
    spaceos-modules-cabinet/        (NuGet lib)
    spaceos-modules-contracts/      (NuGet lib)
    spaceos-nesting-algorithms/     (NuGet lib)
    local-nuget/
  frontend/
    joinerytech-portal/             ← EGYETLEN frontend (újraépítés alatt)
  docs/                             ← dokumentáció (érintetlen)
  keycloak/                         ← IdP config
  tools/                            ← dispatcher
```

### Törölt mappák/service-ek

| Törölt | Ok |
|---|---|
| `design-portal/` | Frontend reset — újraépül |
| `spaceos-doorstar-portal/` | Frontend reset — beolvad joinerytech-portal-ba |
| `spaceos-freetier-portal/` | Frontend reset — beolvad joinerytech-portal-ba |
| `spaceos-freetier-api/` (5010) | Nesting → beépül a fő portálba |
| `spaceos-partner-api/` (5011) | PartnerTier — későbbre halasztva |
| `spaceos-modules-manufacturing/` (5007) | DEV COMPLETE — nem deployed, későbbre halasztva |
| `spaceos-workers-identity/` (5008) | Üres — későbbre halasztva |
| `e2e/`, `epics/`, `infra/`, `tester/`, `architect/`, `librarian/` | Terminál config-ok — nem kód |
| `agent_tools/`, `vision/`, `logs/` | Elavult |

### Architektúra változás

**VOLT:**
```
Frontend → Orchestrator (BFF proxy) → Backend service-ek
```

**LETT:**
```
Frontend → nginx (proxy) → Backend service-ek     (direkt API hívások)
Frontend → Orchestrator                            (csak LLM/AI hívások)
```

Az Orchestrator NEM proxy többé — **AI gateway** (LLM Tool Calling, chat). Az API proxy-zást nginx végzi.

### Frontend döntés (Manifesztum alapú)

- **Egy app:** `joinerytech.hu`
- **Minden ingyenes:** bejelentkezés = teljes hozzáférés
- **Nincs FreeTier/paid tier szétválasztás**
- **Támogatási formák** (donation) nem oldanak fel funkciót

---

## Rendszer architektúra (ÚJ)

```
Browser  https://joinerytech.hu
  │
  ▼
Nginx       (TLS 1.3 · HSTS · CSP)                          port 443
  │  /                    → frontend/joinerytech-portal/dist/  (SPA)
  │  /api/*               → backend/spaceos-kernel (5000)
  │  /joinery/*           → backend/spaceos-modules-joinery (5002)
  │  /cutting/*           → backend/spaceos-modules-cutting (5005)
  │  /inventory/*         → backend/spaceos-modules-inventory (5004)
  │  /procurement/*       → backend/spaceos-modules-procurement (5006)
  │  /abstractions/*      → backend/spaceos-modules-abstractions (5003)
  │  /ai/*                → backend/spaceos-orchestrator (3000)   ← CSAK AI/LLM
  │  /auth/*              → Keycloak (8080)
  ▼
Backend services          (loopback only, systemd)
```

---

## Service-ek

| Service | Port | Tesztek | Státusz | Path |
|---|---|---|---|---|
| **Kernel** | 5000 | **1178** | ✅ RUNNING | `backend/spaceos-kernel/` |
| **Orchestrator** | 3000 | **254** | ✅ RUNNING | `backend/spaceos-orchestrator/` |
| **Joinery** | 5002 | **389** | ✅ RUNNING | `backend/spaceos-modules-joinery/` |
| **Abstractions** | 5003 | **81** | ✅ RUNNING | `backend/spaceos-modules-abstractions/` |
| **Inventory** | 5004 | **164** | ✅ RUNNING | `backend/spaceos-modules-inventory/` |
| **Cutting** | 5005 | **931** | ✅ RUNNING | `backend/spaceos-modules-cutting/` |
| **Procurement** | 5006 | **53** | ✅ RUNNING | `backend/spaceos-modules-procurement/` |

## NuGet Libraries

| Csomag | Tesztek | Path |
|---|---|---|
| **Cabinet** (10 csomag) | **755** | `backend/spaceos-modules-cabinet/` |
| **Contracts** | **57** | `backend/spaceos-modules-contracts/` |
| **Nesting.Algorithms** | **32** | `backend/spaceos-nesting-algorithms/` |

## Frontend

| App | Domain | Státusz | Path |
|---|---|---|---|
| **JoineryTech Portal** | joinerytech.hu | ✅ REFERENCIA OLDALAK KÉSZ (229 teszt, 0 build hiba) | `frontend/joinerytech-portal/` |

**FE referencia-alapú oldalak kész** (2026-04-30, commit `720e106`):
- Landing page (világos téma, Keycloak CTA)
- Keycloak OIDC PKCE login/logout (sessionStorage stateStore)
- 13+ oldal teljes redesign a design reference alapján
- WorkflowPage kanban, SalesPage funnel+KPI, DesignPage wizard, ShopFloor kiosk rewrite
- Új komponensek: Avatar, SlideOver, ChatPanel, NewOrderDrawer, OffcutsPanel, stb.
- Settings 10 fül: Cég, Felhasználók, Jogosultságok, Telephely, Géppark, Partnerek, stb.
- 229/229 teszt pass, pnpm build 0 error

## Backend tesztek összesen: ~3894

```
Kernel 1178 + Orchestrator 254 + Joinery 389 + Abstractions 81 +
Inventory 164 + Cutting 931 + Procurement 53 + Contracts 57 +
Nesting 32 + Cabinet 755 = 3894
```

---

## Operátori teendők

| # | Feladat |
|---|---|
| nginx config: API proxy route-ok | Még nem konfigurálva — frontend rebuild-del együtt |
| Brevo API key | Még nem regisztrálva |
| Turnstile site key | Még nem regisztrálva |
