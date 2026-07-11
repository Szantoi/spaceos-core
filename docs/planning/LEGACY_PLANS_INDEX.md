# SpaceOS LEGACY PLANS INDEX
> Tervdokumentumok katalógusa: 2026-04-07 — 2026-05-28
> Forrás: `/opt/spaceos/docs/spaceos_design_migration/SpaceOS_docs/tervek/`
> Generálva: 2026-06-17

---

## Összefoglaló

| Kategória | Terv db | Státusz | Effort (nap) |
|-----------|---------|--------|--------------|
| **Cabinet** | 2 | ✅ IMPLEMENTÁCIÓRA KÉSZ | 13–15 |
| **Cutting** | 2 | ✅ IMPLEMENTÁCIÓRA KÉSZ | 13.5–16.5 |
| **Portal/Frontend** | 2 | ✅ IMPLEMENTÁCIÓRA KÉSZ | 16 |
| **Joinery** | 2 | ✅/🟠 IMPLEMENTÁCIÓ INDÍTVA | 16 + 2.5–3 |
| **Identity/Keycloak** | 2 | ✅ IMPLEMENTÁCIÓRA KÉSZ | 17 + 12 |
| **Sales** | 1 | ✅ IMPLEMENTÁCIÓRA KÉSZ | 20.5 |
| **Abstractions** | 1 | ✅ IMPLEMENTÁCIÓRA KÉSZ | 46 |
| **Phase 3B** | 2 | ✅ IMPLEMENTÁCIÓRA KÉSZ | 14 |
| **Manufacturing** | 1 | 🟠 design complete | 22–26 |
| **Supporting** | 4 | 🟡 context docs | — |
| **Implementation** | 29+ | 📊 snapshot históriák | — |
| **TOTAL** | 60+ | — | **~165–190 fejlesztői nap** |

---

## Cabinet Modul Tervek

### 1. Cabinet 0.3 Federation Architecture (v4.0)
- **Fájl:** `SpaceOS_Cabinet_0_3_Federation_Architecture_v4.md`
- **Verzió:** v4.0 — 2026-04-27
- **Státusz:** ✅ IMPLEMENTÁCIÓRA KÉSZ
- **Blokkoló:** Cabinet 0.2 ✅ (518 teszt)
- **Főbb témák:**
  - Catalog Federation (Shared + Community)
  - TenantStandard Aggregate
  - ConstructionRuleEngine párhuzamosítás (Parallel.ForEachAsync)
  - CatalogEntryCluster + SimilarityFingerprint
  - Community rating + flag system (3+ flag → soft-hide)
  - RLS policy hardening (DB-01 CRITICAL fix: `rating_read_published`)
  - Fingerprint poisoning probation (7 nap)
- **Becsült effort:** 13–15 fejlesztői nap
- **Migration sorszám:** 0027-0030

### 2. Cabinet 0.3 Implementation README
- **Fájl:** `PHASE_CABINET_03_README.md`
- **Cél:** Claude Code agent context document
- **Implementáció:** 6-8 nap sprint, napi feladatok lebontva

---

## Cutting Modul Tervek

### 1. Cutting Phase 6: Adapters Architecture (v4.0)
- **Fájl:** `SpaceOS_Modules_Cutting_Phase6_Adapters_Architecture_v4.md`
- **Verzió:** v4.0 — 2026-04-28
- **Státusz:** ✅ IMPLEMENTÁCIÓRA KÉSZ
- **Blokkoló:** Cutting Phase 1-5 ✅
- **Főbb témák:**
  - OptiCut, CutRite, Manual adapter integration
  - Append-only audit log (`adapter_call_audit`)
  - Retention/partition policy (90/365 nap)
  - Path traversal RLS (SEC-01 CRITICAL)
  - XXE hardening, SSRF allowlist (SEC-02/SEC-03 CRITICAL)
  - DoS: bulk poll worker + bounded channel (1000 cap, 10 consumer)
  - Cache invalidation (Redis pub/sub 30s TTL)
- **Becsült effort:** 13.5–16.5 fejlesztői nap

### 2. Cutting Phase 5: Analytics Architecture (v4 README)
- **Fájl:** `SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4_README.md`

---

## Portal / Frontend Tervek

### 1. Phase 3C: Multi-Brand Portal Architecture (v2.0)
- **Fájl:** `SpaceOS_Phase3C_Architecture_v2.md`
- **Verzió:** v2.0 — 2026-04-07
- **Státusz:** ✅ IMPLEMENTÁCIÓRA KÉSZ
- **Blokkoló:** Phase 3B ✅
- **Főbb témák:**
  - Turborepo monorepo migráció (apps/ + packages/)
  - Brand skin system (doorstar.tsx override, BrandProvider)
  - `@spaceos/brand-tokens` (adat-only, React-mentes)
  - Tailwind preset CSS var-ok alapján
  - JWT spoofing fix (SEC-07 CRITICAL)
- **Becsült effort:** 16 fejlesztői nap

### 2. Phase 3C Implementation README
- **Fájl:** `PHASE_3C_README.md`
- **Célok:** joinerytech + Doorstar brand pilot

---

## Joinery Modul Tervek

### 1. Joinery v2: Claude Code Package (v2.0)
- **Fájl:** `SpaceOS_Joinery_v2_Claude_Code_Package.md`
- **Verzió:** v2.0 — 2026-04-10
- **Státusz:** 🟠 IMPLEMENTÁCIÓ INDÍTVA
- **Főbb témák:**
  - PDF gyártásilap generálás (QuestPDF 2024.12)
  - Graph Engine → szabászlista kalkulus
  - Data Sovereignty axiom (PDF renderelés csak L2-ben)
  - 6 track, 3 repo (Joinery, Abstractions, Orchestrator)
- **Becsült effort:** 16 fejlesztői nap
- **Golden Rules:** 12 db (DDD, domain events, Specification, Result<T>)

### 2. Joinery Order Conversion Receiver (v2.0)
- **Fájl:** `SpaceOS_Joinery_OrderConversionReceiver_Architecture_v2.md`
- **Verzió:** v2.0 — 2026-05-28
- **Státusz:** ✅ IMPLEMENTÁCIÓRA KÉSZ
- **Főbb témák:**
  - ADR-039 write-ág: Quote → Order konverzió
  - `POST /joinery/internal/orders/from-quote` endpoint
  - Idempotency key: `(TenantId, SourceQuoteId)`
  - RLS `FORCE` új `DoorOrderConvertedLines` táblán (SEC-02 CRITICAL)
- **Becsült effort:** 2.5–3.0 fejlesztői nap
- **Migration sorszám:** J-003

---

## Identity & Keycloak Tervek

### 1. Identity Module Architecture (v4.0)
- **Fájl:** `SpaceOS_Modules_Identity_Architecture_v4.md`
- **Verzió:** v4.0 — 2026-05-27
- **Státusz:** ✅ IMPLEMENTÁCIÓRA KÉSZ
- **Blokkoló:** P0-1 (JWT RS256) előbb
- **Főbb témák:**
  - Tenant-scoped user management
  - Keycloak Admin API integration
  - Write-through outbox pattern (KcSyncWorkerService)
  - `SpaceOSUser` aggregate → Keycloak sync
  - Rate-limit: 5 reset/user/hour, Redis sliding window
- **Becsült effort:** ~17 fejlesztői nap

### 2. Keycloak IdP Integration Architecture (v4.0)
- **Fájl:** `SpaceOS_Keycloak_IdP_Architecture_v4.md`
- **Verzió:** v4.0 — 2026-04-09
- **Státusz:** ✅ IMPLEMENTÁCIÓRA KÉSZ
- **Főbb témák:**
  - Authorization Code + PKCE + state + nonce (SEC-01/SEC-02 CRITICAL)
  - 1 realm `spaceos`, tenant = Keycloak Group
  - Portal hosted login page
  - JWKS cache 10 min, key rotation fallback
- **Becsült effort:** ~12 fejlesztői nap
- **Keycloak verzió:** pin to 24.0.5

---

## Sales Modul Terv

### 1. Sales Module Architecture (v4.0)
- **Fájl:** `SpaceOS_Modules_Sales_Architecture_v4.md`
- **Verzió:** v4.0 — 2026-05-27
- **Státusz:** ✅ IMPLEMENTÁCIÓRA KÉSZ — 34 finding lezárva, 0 CRITICAL/HIGH maradék
- **Blokkoló:** Joinery idempotent receiver ✅, Keycloak IdP ✅, Modules.Identity ✅
- **Főbb témák:**
  - Customer/Lead CRM + Quote lifecycle
  - Quote → Order conversion (ADR-039)
  - B2BHandshake customer link verification (SEC-S-02 CRITICAL)
  - Quote immutability trigger
  - `quote_number_counters` race-free generálás (pg_advisory_xact_lock)
  - Outbox pattern SalesIntegrationWorker-vel
- **Becsült effort:** 20.5 nap
- **DB schema:** `spaceos_sales`
- **Port:** 5009

---

## Abstractions Modul Terv

### 1. Abstractions: Product Configuration Engine (v4.0)
- **Fájl:** `SpaceOS_Modules_Abstractions_Architecture_v4.md`
- **Verzió:** v4.0 — 2026-04-09
- **Státusz:** ✅ IMPLEMENTÁCIÓRA KÉSZ
- **Főbb témák:**
  - Parametric Component Graph
  - ProductTemplate + ComponentSlot + SlotConnection DAG
  - RuleOperator enum (zárt: Identity, Subtract, Add, SubtractN, Max, Min, Constant)
  - GeometryAttachment (L0-L4 fidelity levels)
  - IFC/STEP parser (sandbox process, max 50MB, 100K elem, 60s timeout)
  - DAG cycle detection (recursive CTE)
- **Becsült effort:** ~46 fejlesztői nap
- **DB schema:** `spaceos_modules`

---

## Phase 3B Terv

### 1. Phase 3B: Escrow GA Foundation Architecture (v4.0)
- **Fájl:** `SpaceOS_Phase3B_Architecture_v4.md`
- **Verzió:** v4.0 — 2026-04-07
- **Státusz:** ✅ IMPLEMENTÁCIÓRA KÉSZ
- **Főbb témák:**
  - AggregateSnapshot store (StateJson + SnapshotHash)
  - Outbox pattern (event → background worker)
  - ProofHash + WORM storage (S3 Object Lock / Azure Immutable Blob)
  - Audit chain integrity verification
- **Becsült effort:** 14 fejlesztői nap
- **Migration sorszám:** 0020–0023

### 2. Phase 3B Implementation README
- **Fájl:** `PHASE_3B_README.md`

---

## Manufacturing Modul Terv

### 1. Manufacturing Phase 1 Architecture (v4.0 README)
- **Fájl:** `SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4_README.md`
- **Verzió:** v4.0 — 2026-04-28
- **Státusz:** 🟠 design complete
- **Főbb témák:**
  - EdgeBandingTask + CncTask + ManufacturingOrder saga
  - Cross-module Inbox (Cutting `PanelCompleted` event)
  - Workers.Identity module (separate repo)
  - KEK rotation (90 nap, HKDF-SHA256)
- **Becsült effort:** 22–26 nap
- **Érintett repók:** `spaceos-modules-manufacturing` (új), `spaceos-workers-identity` (új)

---

## ADR Hivatkozások

| ADR | Terv | Téma |
|-----|------|------|
| ADR-011 | Keycloak IdP | 1 realm, tenant = Group |
| ADR-014 | Abstractions | Product Graph Engine |
| ADR-015 | Abstractions | Zárt RuleOperator enum |
| ADR-016 | Abstractions | Multi-Fidelity Geometry (L0-L4) |
| ADR-017 | Abstractions | CAD Feature Tree = BOM = Process |
| ADR-024 | Sales, Cutting | BYPASSRLS worker pattern |
| ADR-039 | Sales, Joinery | Quote→Order conversion (write-ág) |

---

## Kritikus Security Findings

| CRITICAL finding | Terv | Megoldás |
|---|---|---|
| RLS bypass (`app.is_cabinet_moderator`) | Cabinet 0.3 | Role-based migration (SET ROLE) |
| Fingerprint spoofing | Cabinet 0.3 | Server-side enforcement + BEFORE trigger |
| File path traversal | Cutting Phase 6 + Abstractions | Regex validáció + storage prefix |
| XXE (Billion laughs attack) | Cutting Phase 6 | XmlReaderSettings {Prohibit, null resolver} |
| SSRF (169.254.169.254 metadata) | Cutting Phase 6 | Allowlist + DNS private range check |
| PKCE state param (session fixation) | Keycloak IdP | Crypto random state + sessionStorage |
| OIDC nonce (replay attack) | Keycloak IdP | Nonce generálás + id_token validáció |
| JWT spoofing | Phase 3C | BFF response body, RS256 verify |
| RLS on DoorOrderConvertedLines | Joinery receiver | FORCE ROW LEVEL SECURITY + policy |
| Cross-tenant link forgery | Sales | B2BHandshake LinkVerificationStatus |
| ProofHash WORM key cross-tenant | Phase 3B | TenantId path prefix enforcement |

---

## Megvalósítás Dokumentumok

### Codebase Status Históriák
`megvalósítás/` mappa — 29 fájl, 2026-04-07 — 2026-05-27

| Dátum | Fájl | Megjegyzés |
|-------|------|-----------|
| 2026-04-07 | `Codebase_Status_20260407.md` | v1 |
| 2026-04-13 | `Codebase_Status_20260413.md` | Átalakítás befejezve |
| 2026-05-27 | `Codebase_Status_20260527.md` | **LATEST** |

### Architecture Risk Review
- `ArchitectureRiskReview_20260413.md` — Round 1
- `ArchitectureRiskReview_Round2_20260413.md` — Round 2
- `ArchitectureRiskReview_Response_*.md` — Válaszok

### Egyéb
- `TECH_DEBT.md` — Műszaki adósság
- `SpaceOS_SprintC_Charter_v4.md` — Sprint C charter
- `SpaceOS_Sprint_D_Phase1-1.md` — Sprint D Phase 1

---

## Verziózás & Review Pipeline

Minden fő terv az alábbi pipeline-t követi:
1. **v1 Draft** — Architect sketch
2. **v2** — `/database-designer` + `/database-schema-designer` review
3. **v3** — `/senior-security` review
4. **v4** — `/senior-backend` review
5. **✅ IMPLEMENTÁCIÓRA KÉSZ** — v4 final, 0 CRITICAL maradék
