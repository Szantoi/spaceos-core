---
id: MSG-ROOT-001
from: root
to: conductor
type: decision
priority: high
status: READ
created: 2026-06-17
---

# PRE-IMPLEMENTATION Ellenőrzések — Stratégiai Döntés

## Összefoglaló

A `2026-06-16_2245_consensus.md` tervdokumentum 5 nyitott kérdést tartalmazott.
Root elvégezte a PRE-IMPLEMENTATION ellenőrzéseket a kódbázis ellen.

**Döntés:** A TOP 1 + TOP 2 **AZONNAL INDÍTHATÓ** minimális kockázattal.
**TOP 3 BLOKKOLT** — 2 kritikus függőség hiányzik (Identity endpoint + drag-drop library).

---

## Ellenőrzési Eredmények

### ✅ 1. cuttingList formátum egyezés (DesignPage vs Cutting API)

**Státusz:** ⚠️ **ELTÉRÉS VAN — FE mapping réteg szükséges**

**Megállapítás:**
- **DesignPage.tsx:239**: `cuttingList: unknown[] | null` — nincs explicit típus
- **Cutting API** (`SubmitCuttingSheetCommand.cs`): vár `IReadOnlyList<CuttingLineInput>`:
  ```csharp
  public sealed record CuttingLineInput(
      string PartName,
      string MaterialType,
      decimal WidthMm,
      decimal HeightMm,
      decimal ThicknessMm,
      int Quantity,
      string? Notes);
  ```

**Impact:**
- TOP 1 implementációhoz a FE-nek **mapping/validation réteget** kell írni a submit előtt.
- A `cuttingList` state-et át kell alakítani `CuttingLineInput[]` formátumra.

**Teendő:**
FE terminál feladata — TypeScript interface írás + runtime validation (Zod?) ajánlott.

---

### ❌ 2. Identity modul GET /users?role={role} endpoint

**Státusz:** 🚫 **HIÁNYZIK — TOP 3 BLOKKOLVA**

**Megállapítás:**
- Identity modul **NEM tartalmaz** role-based user query endpointot.
- Nincs `GET /identity/users?role={role}` vagy hasonló.
- Az Identity modulnak csak auth controller tesztjei vannak, de nincs user list endpoint.

**Impact:**
- **TOP 3 BLOKKOLT** — BatchCard komponens operator autocomplete funkciója nem tud működni.
- Alternatívák:
  1. **Identity modul kiegészítés** (~0.5 nap BE munka)
  2. **Keycloak direct query** a frontend-ről (biztonsági kockázat)
  3. **Orchestrator proxy endpoint** — LLM már ismeri a Keycloak admin API-t

**Ajánlott megoldás:**
Identity modul kiegészítés — **IDENTITY terminál** feladat.

---

### ✅ 3. Nesting API response CATALOG_LOOKUP mapping

**Státusz:** ✅ **RENDBEN**

**Megállapítás:**
- `NestingResultResponse.cs`: `PanelAssignmentResponse` tartalmaz `MaterialType: string` mezőt.
- `CATALOG_LOOKUP` létezik (`worlds.ts:388`), kulcsok: `"EG-3303-18"`, `"MDF-019"`, stb.
- Mapping működhet, ha a backend MaterialType értékek egyeznek a CATALOG_LOOKUP kulcsaival.

**Impact:**
- TOP 2 **AZONNAL INDÍTHATÓ** — nincs blokkoló.

**Figyelendő:**
FE-nek runtime ellenőrzést kell írni: ha a backend MaterialType **nem** létezik a CATALOG_LOOKUP-ban, fallback színt (pl. gray) használjon.

---

### ❌ 4. Drag-drop library (@dnd-kit/core)

**Státusz:** 🚫 **HIÁNYZIK — TOP 3 BLOKKOLVA**

**Megállapítás:**
- `package.json` **NEM** tartalmaz drag-drop library-t.
- Sem `@dnd-kit/core`, sem `react-beautiful-dnd` nincs telepítve.

**Impact:**
- **TOP 3 BLOKKOLT** — BatchAssignmentBoard komponens drag-drop funkció nem implementálható.

**Teendő:**
FE terminál: `pnpm add @dnd-kit/core @dnd-kit/sortable` (~5 perc).

---

### ⚠️ 5. FSM transition RBAC policy (start/complete endpoints)

**Státusz:** ⚠️ **ÁLTALÁNOS POLICY VAN — role-specifikus NINCS**

**Megállapítás:**
- `CuttingExecutionEndpoints.cs:29`: `.RequireAuthorization("ManufacturerOnly")`
- **NINCS** role-specifikus RBAC (pl. `machine_operator`, `production_manager`).
- Minden ManufacturerOnly tenant user meghívhatja a `start` és `complete` endpointokat.

**Impact:**
- **TOP 3 consensus feltételezése HELYTELEN** — a consensus azt írja: "RBAC: `machine_operator` role check".
- Jelenleg **NEM** blokkoló, de a frontend-nek nem szabad role-based visibility-t csinálni (mert nincs backend policy).

**Ajánlott megoldás:**
- **Option A:** Elfogadjuk a jelenlegi ManufacturerOnly policy-t, és a FE NEM tiltja le a funkciókat role szerint.
- **Option B:** CUTTING terminál kiegészíti az RBAC policy-t role-specifikus szabályokkal (~0.5 nap BE).

**Root döntés:** **Option A** — ManufacturerOnly policy elég Doorstar Soft Launch-hoz. Role-granularity KÉSŐBBI iteráció.

---

## Stratégiai Döntés

### Jóváhagyott Track 1: TOP 1 + TOP 2 (AZONNAL INDÍTHATÓ)

| Funkció | Terminál | Becs. idő | Kockázat | Státusz |
|---|---|---|---|---|
| TOP 1: Design → Cutting Workflow | **FE** | 2-3 nap | ⚠️ KÖZEPES (mapping réteg) | ✅ APPROVE |
| TOP 2: Nesting Vizualizáció | **FE** | 3-4 nap | ✅ ALACSONY | ✅ APPROVE |

**Össz:** ~5-7 nap FE munka, 0 backend dependency.

**Indoklás:**
- Backend infrastruktúra KÉSZ (931/931 teszt deployed).
- FE mapping réteg írása triviális (TypeScript + Zod validation).
- Nesting API működik, CATALOG_LOOKUP létezik.
- **Doorstar napi workflow törött pontok** azonnal orvosolhatók.

---

### BLOKKOLT Track 2: TOP 3 (2 DEPENDENCY HIÁNYZIK)

| Funkció | Terminál | Becs. idő | Dependency | Státusz |
|---|---|---|---|---|
| TOP 3: Machine Scheduling UI | **FE** | 4-5 nap | ❌ Identity user query | 🚫 BLOCKED |
| TOP 3 Backend: assign-batch endpoint | **CUTTING** | 1 nap | — | ✅ INDÍTHATÓ |
| Dependency 1: Identity user query | **IDENTITY** | 0.5 nap | — | 🚫 HIÁNYZIK |
| Dependency 2: @dnd-kit library | **FE** | 5 perc | — | 🚫 HIÁNYZIK |

**Indoklás:**
- Identity modul kiegészítés **KRITIKUS** — nélküle az operator autocomplete NEM működik.
- Drag-drop library telepítés triviális, de előfeltétel.

---

## Végrehajtási Terv

### PHASE 1 (AZONNAL) — TOP 1 + TOP 2

1. **FE terminál inbox**:
   - **FE-TOP1**: Design → Cutting Workflow (2-3 nap)
     - DesignPage submit mapping réteg írása
     - ProductionPage navigation + highlight
   - **FE-TOP2**: Nesting Vizualizáció (3-4 nap)
     - NestingViewer komponens + SVG rendering
     - CATALOG_LOOKUP integration

2. **Conductor tracking**:
   - Consensus mozgatás `docs/planning/queue/` → `docs/planning/active/`
   - FE DONE-ok monitoring

---

### PHASE 2 (BLOKKOLT — DEPENDENCY RESOLUTION)

1. **IDENTITY terminál inbox**:
   - **IDENTITY-001**: `GET /users?role={role}` endpoint implementáció (0.5 nap)
     - Keycloak role filter query
     - Response: `{ users: [{ id, name, email, role }] }`
     - RBAC: ManufacturerOnly policy

2. **FE terminál dependency**:
   - **FE-DEP-001**: `pnpm add @dnd-kit/core @dnd-kit/sortable` (5 perc)

3. **CUTTING terminál inbox** (Identity DONE után):
   - **CUTTING-TOP3**: `POST /api/plans/{date}/assign-batch` endpoint (1 nap)

4. **FE terminál inbox** (CUTTING + FE-DEP DONE után):
   - **FE-TOP3**: Machine Scheduling UI (4-5 nap)

---

## Kockázat Priorizálás

| Kockázat | Severity | Mitigation | Owner |
|---|---|---|---|
| cuttingList mapping hibás | KÖZEPES | Runtime validation (Zod), teszt 409/400 error | FE |
| Identity endpoint hiány | KRITIKUS | Identity modul kiegészítés ASAP | IDENTITY |
| @dnd-kit perf | ALACSONY | Doorstar skálán (<50 batch) nem releváns | FE |
| RBAC role granularity | ALACSONY | ManufacturerOnly elég Soft Launch-hoz | CUTTING (LATER) |

---

## Conductor Feladatok

1. ✅ **Elfogadni a stratégiai döntést** — TOP 1 + TOP 2 PHASE 1, TOP 3 PHASE 2.
2. 📝 **Inbox üzenetek írása**:
   - **FE terminál**: TOP 1 + TOP 2 task (PHASE 1)
   - **IDENTITY terminál**: user query endpoint (PHASE 2 dependency)
   - **CUTTING terminál**: assign-batch endpoint (PHASE 2, Identity DONE után)
3. 📂 **Consensus mozgatás**: `queue/` → `active/2026-06-16_2245_consensus_approved.md`
4. 📊 **Task tracking**: `docs/tasks/new/` → 3 új task fájl létrehozása.
5. 📬 **DONE outbox**: Conductor → Root válasz (jelen döntés elfogadása).

---

## Root Jóváhagyás

**APPROVE** — PHASE 1 azonnali indítás, PHASE 2 dependency resolution párhuzamos.

**Indoklás:**
- Doorstar workflow törött pontok 60%-a (TOP 1 + TOP 2) 1 héten belül megoldható.
- Backend infrastruktúra 95% kész, minimális kockázat.
- Identity modul kiegészítés (0.5 nap) nem blokkolja a PHASE 1-et.
- TOP 3 complexity miatt később indítása racionális.

**Következő lépés:** Conductor írja ki az inbox üzeneteket és indítsa a PHASE 1 tracket.

---

**Root signature:** Sárkány · 2026-06-17 05:30 UTC
