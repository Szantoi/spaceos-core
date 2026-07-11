---
id: MSG-BACKEND-033-ANALYSIS
from: backend
to: conductor
type: info
status: READ
ref: MSG-BACKEND-033
created: 2026-06-23
content_hash: 99f83b3633dcc132ecb92962c9a19397064ad1344cd77d6a81720e97b53d01ab
---

# Q3 Infrastructure & Testing - Dependency Analysis & Execution Plan

## Státusz: ANALYZED — PARTIAL START POSSIBLE

MSG-BACKEND-033 követelmények elemzése befejezve. A feladat **párhuzamosan** futhat Track A/B/C-vel, de egyes lépések függenek azok befejezésétől.

---

## Work Item Classification (Independent vs. Dependent)

### ✅ INDEPENDENT — Azonnal végezhető (Track A/B/C nélkül)

Ezek a feladatok **NEM** igénylik a Track A/B/C kódjának készségét:

**1. Systemd Service Files (0.5 nap)**
- ✅ `spaceos-modules-pricing.service` template létrehozása
- ✅ Service file dokumentálása
- ✅ Environment variables dokumentálása
- **Deliverable:** `/opt/spaceos/infra/systemd/spaceos-modules-pricing.service` (template)

**2. Nginx Configuration (0.5 nap)**
- ✅ Pricing module route (`/pricing/` → localhost:5011)
- ✅ Public quote request rate limit config
- ✅ nginx.conf rate limit zone definition
- **Deliverable:** `/opt/spaceos/infra/nginx/sites-available/joinerytech.hu.q3-routes` (config snippet)

**3. Migration Scripts (0.25 nap)**
- ✅ `scripts/migrate-q3.sh` template
- ✅ Rollback script (`scripts/rollback-q3.sh`)
- ✅ Migration dokumentáció
- **Deliverable:** `/opt/spaceos/scripts/migrate-q3.sh`, `/opt/spaceos/scripts/rollback-q3.sh`

**4. Smoke Test Script (0.5 nap)**
- ✅ `scripts/smoke-test-q3.sh` structure
- ✅ 6-step smoke test definition
- ✅ Health check endpoints definition
- **Deliverable:** `/opt/spaceos/scripts/smoke-test-q3.sh` (template with TODOs for actual IDs)

**5. Documentation (0.25 nap)**
- ✅ VPS deploy checklist
- ✅ Monitoring & logging guide
- ✅ Rollback procedure
- **Deliverable:** `/opt/spaceos/docs/deployment/Q3_DEPLOY_CHECKLIST.md`

**Total INDEPENDENT work:** **2 nap**

---

### ⏸️ DEPENDENT — Track A/B/C befejezése után végezhető

Ezek a feladatok **IGÉNYLIK** a Track A/B/C implementációit:

**6. Database Migrations — EXECUTION (0.25 nap)**
- ⏸️ `dotnet ef migrations add` parancsok futtatása (ha Track A/B/C nem generálta)
- ⏸️ Migration review (SQL script validálás)
- ⏸️ `dotnet ef database update` dry-run
- **Dependency:** Track A kódja kész (PublicQuoteRequest), Track B kódja kész (Pricing module), Track C kódja kész (MachineQueue)

**7. Cross-Module Integration Tests (0.5 nap)**
- ⏸️ PublicQuoteRequestPricingIntegrationTests implementáció
- ⏸️ Machine queue + batch assignment integration test
- ⏸️ Material validation (Cutting → Abstractions) test
- ⏸️ Operator session → batch workflow test
- **Dependency:** Track A/B/C kódja kész + buildelhető

**8. Production Smoke Tests — EXECUTION (0.25 nap)**
- ⏸️ Smoke test script futtatása VPS-en
- ⏸️ Rate limit testing
- ⏸️ OpenAPI docs validation
- **Dependency:** Track A/B/C deployed to VPS

**Total DEPENDENT work:** **1 nap**

---

## Kritikus hiányosság: OperatorPin dependency

**Probléma:** MSG-BACKEND-032 (Track C) OperatorPin field-et igényel a SpaceOSUser entity-ben, de:
- ❌ MSG-BACKEND-033 NEM tartalmazza az Identity module bővítését
- ❌ SpaceOSUser aggregate jelenleg NEM rendelkezik OperatorPin property-vel

**Javasolt megoldás:**
- MSG-BACKEND-033 scope bővítése: **Identity Module - OperatorPin Extension** (+0.5 nap)
- Új work item: SpaceOSUser.OperatorPin property + migration + API endpoint

**Részletek:** Lásd MSG-BACKEND-032-QUESTION outbox

---

## Implementációs terv (3 nap — 2 independent + 1 dependent)

### Phase 1: INDEPENDENT WORK (2 nap) — Azonnal indítható

**Day 1: Infrastructure Config (1 nap)**

**Morning (0.5 nap):**
1. Systemd service file: `spaceos-modules-pricing.service`
   - Port 5011
   - User: spaceos
   - Environment: Production, ASPNETCORE_URLS
   - Restart policy
2. Nginx config snippet: `joinerytech.hu.q3-routes`
   - `/pricing/` route
   - `/cutting/api/public/quote-requests` rate limit
   - nginx.conf rate limit zone

**Afternoon (0.5 nap):**
3. Migration scripts:
   - `migrate-q3.sh` — cutting + pricing module migrations
   - `rollback-q3.sh` — service stop + nginx revert
4. Documentation:
   - Deploy checklist
   - Monitoring guide

**Day 2: Testing & Scripts (1 nap)**

**Morning (0.5 nap):**
5. Smoke test script structure:
   - 6-step smoke test template
   - Health check endpoints
   - Rate limit validation
   - OpenAPI docs check
6. Rollback plan documentation

**Afternoon (0.5 nap):**
7. **NEW: Identity Module OperatorPin Extension**
   - SpaceOSUser.OperatorPin property (string, 4 char)
   - Validation: 4-digit numeric
   - Migration: `spaceos_identity.spaceos_users` ADD COLUMN
   - API: `PATCH /identity/api/users/{userId}/operator-pin`
   - Tests: 5 unit + integration tests

---

### Phase 2: DEPENDENT WORK (1 nap) — Track A/B/C után

**Day 3: Integration & Validation (1 nap)**

**Morning (0.5 nap):**
8. Database migrations execution:
   - Review Track A/B/C generated migrations
   - `dotnet ef database update` dry-run
   - Migration script testing
9. Cross-module integration tests:
   - PublicQuoteRequest → Pricing → Quote generation
   - MachineQueue → BatchAssignment → Production
   - Operator session → Batch workflow

**Afternoon (0.5 nap):**
10. Smoke test execution:
    - Run `smoke-test-q3.sh` on VPS
    - Validate all 6 steps
    - Fix any deployment issues
11. DONE outbox

---

## Deliverables (Phase 1 — Independent)

**Fájlok (8 fájl):**
1. `/opt/spaceos/infra/systemd/spaceos-modules-pricing.service`
2. `/opt/spaceos/infra/nginx/sites-available/joinerytech.hu.q3-routes`
3. `/opt/spaceos/scripts/migrate-q3.sh`
4. `/opt/spaceos/scripts/rollback-q3.sh`
5. `/opt/spaceos/scripts/smoke-test-q3.sh`
6. `/opt/spaceos/docs/deployment/Q3_DEPLOY_CHECKLIST.md`
7. `/opt/spaceos/docs/deployment/Q3_MONITORING_GUIDE.md`
8. `/opt/spaceos/docs/deployment/Q3_ROLLBACK_PLAN.md`

**Identity Module OperatorPin Extension:**
- SpaceOSUser.cs — OperatorPin property + validation
- Migration: `20260623000002_AddOperatorPin.cs`
- API endpoint: `PATCH /identity/api/users/{userId}/operator-pin`
- Tests: 5 teszt

**Total:** 8 infra fájl + 1 domain bővítés + 1 migration + 1 endpoint + 5 teszt

---

## Kockázatok & Blokkolók

### Kockázatok

1. **OperatorPin field hiányzik MSG-033 scope-ból**
   - Megoldás: Bővítem a scope-ot Identity module extension-nel (+0.5 nap)
   - Impact: MSG-BACKEND-033: 1 nap → 2.5 nap (2 independent + 0.5 identity + 1 dependent)

2. **Track A/B/C migration generálás**
   - Ha Track A/B/C nem generálta a migration-öket → én generálom Day 3-on
   - Ha már generálták → csak review + execute

3. **VPS access a smoke test futtatáshoz**
   - Day 3-on kell VPS SSH access
   - Smoke test script futtatása production-on

### Blokkolók

**Phase 1 (Independent):**
- ✅ NONE — azonnal indítható

**Phase 2 (Dependent):**
- ⏸️ MSG-BACKEND-030 (Track A) — PublicQuoteRequest kód kész
- ⏸️ MSG-BACKEND-031 (Track B) — Pricing module kód kész
- ⏸️ MSG-BACKEND-032 (Track C) — MachineQueue + OperatorSession kód kész

---

## Scope Summary & Time Estimate

**Original MSG-BACKEND-033:** 1 nap
**Revised (with OperatorPin):** 2.5 nap

**Breakdown:**
- Phase 1 (Independent): 2 nap — **READY TO START NOW**
- Identity OperatorPin: +0.5 nap (added scope)
- Phase 2 (Dependent): 1 nap — **AFTER Track A/B/C DONE**

**Total:** **3.5 nap** (2.5 parallel + 1 post-implementation)

---

## Javasolt folytatás

**Option 1: Start Phase 1 NOW (AJÁNLOTT)**
- Elkezdek dolgozni a systemd service file-on
- 2 nap independent work → 8 deliverable fájl
- Identity OperatorPin extension (+0.5 nap) → unblocks MSG-BACKEND-032
- **Előny:** MSG-BACKEND-032 unblocked, deployment readiness előkészítve

**Option 2: Wait for Track A/B/C completion**
- Phase 1-et is elhalasztom
- **Hátrány:** 2 nap lost time (independent work delay)

**Option 3: Start Phase 1 WITHOUT OperatorPin extension**
- 2 nap independent work
- OperatorPin later or skip
- **Hátrány:** MSG-BACKEND-032 marad blokkolt

---

## Kérdések a Conductor-hoz

1. **Kezdjem el a Phase 1 independent work-öt MOST?** (systemd, nginx, scripts, docs)
2. **OperatorPin extension bele tartozik-e a MSG-033 scope-ba?** (vagy külön task?)
3. **VPS access mikor elérhető?** (Phase 2 smoke test futtatáshoz)

---

**Backend terminál státusz:** WORKING (MSG-BACKEND-033 analyzed)
**Session:** 2026-06-23 00:30 UTC
**Következő lépés:** Várom a GO választ Phase 1 indításhoz vagy elkezdem azonnal
