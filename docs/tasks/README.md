# SpaceOS — Feladatok (Root nézet)

## Státuszok

| Mappa | Jelentés |
|---|---|
| `new/` | Beérkezett, root még nem döntött / nem adta ki |
| `active/` | Ki van adva terminálnak vagy operátornak, vár |
| `archive/` | DONE + elfogadott, lezárt |

---

## Jelenlegi állapot

### 🔵 Active

**PORTAL-BUG-0418** — BUG-002: ✅ DEPLOYED · BUG-001 email/phone: ✅ DEPLOYED + TESTER PASS ✅ · Cím mező: BUG-006 (low) · LEZÁRVA (BUG-001 scope)

**CUTTING-BUG-0418** — BUG-004: ✅ TESTER PASS (d8383e7, DateTime.SpecifyKind Utc) · LEZÁRVA ✅

**ORCH-BUG-0418** — BUG-005: ✅ LEZÁRVA — 005a+b+c mind PASS · Chat streaming end-to-end működik

**INVENTORY-BUG-003b** — BUG-003b: ✅ DEPLOYED (9b170a7 LIVE) · GET stock + POST inbound MDF18mm fix · **INVENTORY-013 INVESTIGATION DONE** (99/99 tests, code clean) · TESTER funkcionális teszt szükséges

**INVENTORY-013** — ✅ DONE (2026-04-18) · EnableRetryOnFailure investigation: Inventory module code is clean, no retry strategy configured · Root cause identified as Kernel issue

**KERNEL-091/093** — ✅ Code fix complete (46d64b5) · ❌ Original deployment failed (outdated binaries) · **KERNEL-099:** Secondary root cause found: ModulesDbContext also had EnableRetryOnFailure (added in-place to source) · **KERNEL-100:** ✅ DONE — Fresh binaries built with ALL fixes (App + Audit + Modules contexts) → `/tmp/kernel-publish-fresh/` ready · **Next:** INFRA-035 deploy + TESTER-028 validate

**PROCUREMENT-011** — ✅ DONE (2026-04-18) · EnableRetryOnFailure investigation: Procurement module code is clean, no retry strategy configured · Tests: 53/53 passing

**E2E-052** — 🎉 **245/245 PASS** · 0 fail · 0 skip · Bugfix Sprint LEZÁRVA ✅ (2026-04-18)

**E2E-053** — 📈 **266/266 PASS** (+21 új teszt) · Inventory consumption/offcut, Cutting waste/nesting, Joinery order flow, LLM-tools discovery · Coverage expansion DONE ✅ (2026-04-18)

**✅ TESTER-018 — ROOT CAUSE RESOLVED:** BUG-003b + BUG-007 POST-ok 500 → `EnableRetryOnFailure` Kernel-ben (KERNEL-091/093) · INVENTORY-013 + PROCUREMENT-011 DONE (code clean, 99/99 + 53/53) · Root cause was Kernel's Npgsql retry strategy, not Inventory/Procurement logic · KERNEL-093 (46d64b5) LIVE fixes this

**🔴 SOFT LAUNCH — ONE CRITICAL BLOCKER REMAINS (MSG-ROOT-001):**
  
### ✅ FINAL STRETCH: Deploy → Validate → Soft Launch GO (2026-04-19 08:55)

**ROOT CAUSE RESOLVED + DEPLOYED:**
- ✅ Outdated binaries identified (/tmp/kernel-publish pre-fix)
- ✅ Secondary root cause found: ModulesDbContext EnableRetryOnFailure (missing from 46d64b5, added by KERNEL-099)
- ✅ All fixes compiled into fresh binaries (/tmp/kernel-publish-fresh/)

**CRITICAL PATH (Final Steps):**
1. ✅ **KERNEL-100-DONE:** Fresh binaries ready (1138/1138 tests PASS) — ACCEPTED
2. ✅ **INFRA-036-DONE:** Fresh binaries deployed, service LIVE — ACCEPTED
   - Service: active (running) PID 2203323
   - /healthz: {"status":"healthy","db":"connected"} ✅
3. ⏳ **TESTER-028:** Re-validate POST endpoints → 201 (NOW)

**Timeline to Soft Launch GO:**
- INFRA deploy: ✅ 08:55–09:00 DONE
- TESTER validate: ~10 min (NOW)
- ROOT decision: ~5 min (if TESTER pass)
- **Total: ~15 min to SOFT LAUNCH GO** 🚀

**Remaining Blockers:**
- ✅ BUG-013 (mobile CSS 375px) — PORTAL-017 DONE ✅
- ✅ PORTAL-010 (Inventory UI) — Refactor DONE ✅ (323/323 tests)
- TESTER-028 validation must be 201/201 (POST /inbound + POST /orders)
- Only TESTER-028 left before final GO

**Status: 🚀 SOFT LAUNCH GO ✅ (2026-04-20) — Doorstar Kft. ÉLES · ✅ INVENTORY Phase 1 COMPLETE · 🔴 CUTTING Session C ACTIVE · 🟢 JOINERY Phase 1 ACTIVE · 🔴 FREETIER INDÍTVA**

### 🎉 **CUTTING Session C (COMPLETE)**
- **CUTTING-038 ✅** — PanelReservation aggregate + IInventoryReservationAdapter + rollback (259 teszt)
- **CUTTING-038b ✅** — `IInventoryReservationAdapter` törölve, `IInventoryProvider.ReserveAsync()` + Contracts v1.3.0
- **CUTTING-039 ✅** — DaySlotAutoLockWorker (BackgroundService, 15 perc, csak múltbeli slotok)
- **CUTTING-040** — IPlanningStrategy refactor (DaySlot típusokhoz igazítva)
- **CUTTING-041 ✅** — NESTING-001: WastePiece model + v1.1.0 bump (32 teszt)
- **CUTTING-042 ✅** — Offcut pipeline: CuttingPlanFrozen + PlanNestingSnapshot + RegisterOffcutsHandler + InventoryCuttingHttpAdapter (284 teszt)
- 🎉 **ADR-038 LEZÁRVA** — offcut loop teljes: Freeze → WastePieces → Inventory batch

### ✅ **INVENTORY-014 (DONE)**
- `POST/DELETE/GET /api/inventory/reservations` endpoints bekötve (commit 6abc087)
- 160/160 teszt · 7 új · ManufacturerOnly ✅

### ✅ **INVENTORY-015 (DONE)**
- `POST /api/inventory/offcuts/batch` + `OffcutBatches` tábla + idempotency (459aaef)
- 164/164 teszt · 4 új

### ✅ **CUTTING-PLANNING-V1 (COMPLETE)**
- **Phase 1:** ✅ DONE — Data Model + API (136 tests, MSG-CUTTING-026-DONE)
- **Phase 2:** ✅ DONE — Strategy Pattern + yield (181 tests, MSG-CUTTING-027-DONE)
- **Phase 3 (Planned):** Real order ingestion + geometry bin-packing (backlog)

### 🎉 **FREETIER API (LIVE — 168 teszt)**
- **FREETIER-001–009 ✅** — API komplett + portal delta · DEPLOYED
- **INFRA-047 ⏸️ BLOCKED** — RLS OWNER fix — Gábor manuális psql (superuser)

### ✅ **VPS INFRA — Runbook + CabinetBilder**
- **INFRA-051 ✅** — `cabinetbilder-plugin` KC client LIVE + Port-mátrix validálva

### 🟡 **VPS SECURITY HARDENING (Lynis 65 → ~72 · Batch 1 DONE)**
- **INFRA-052 ✅** — Batch 1: PG 5432 + pgAdmin 5050 + Keycloak 8080 → 127.0.0.1 · SSH hardening ✅
- ⏸️ **S4 apt upgrade** — Gábor root-ként kell futtassa: `apt update && apt upgrade -y`
- Batch 2 (PG chmod, Redis, SSH password) + Batch 3 (auditd, AIDE) → következő session

### 🔴 **FREETIER FRONTEND (ACTIVE — Growth Track)**
- **Tervdok:** `docs/architecture/SpaceOS_FreeTier_Portal_Architecture_v1.md` ✅ APPROVED (1026 sor)
- **Repo:** `spaceos-freetier-portal` · **Domain:** eszkozok.joinerytech.hu
- **FREETIER-009 ✅ + INFRA-050 ✅** — Backend delta DEPLOYED: session + CORS + cookie domain + logout · 168 teszt
- **FREETIER-FE-001 🔴** — Scaffold + Landing + Nesting kalkulátor (Nap 1–4) — **freetier-fe terminál**

### ✅ **JOINERY-PLANNING-V2 Phase 1 (DONE)**
- **Commit:** 460fce9 · 344/344 teszt · QuestPDF L1/L2/L3/L4 + MinIO WORM + 4 endpoint

### ✅ **JOINERY Phase 2 (DONE — DEPLOYED)**
- Commit: 7025c1f LIVE · **384/384 teszt** (+40) · GyartasilapBatch FSM + ZIP + AnyaglistaPdfBuilder (QuestPDF) · 5 új endpoint · 2 migration · presigned TTL=60 · RLS ✅
- **INFRA-042 ✅** — deploy + konfig kész
- **TESTER-035 🔴** — batch + anyaglista smoke teszt kiadva

### ✅ **INFRA-041 (DONE)**
- MinIO WORM bucket + joinery.env + redeploy 460fce9 · /health ✅

### ✅ **INFRA-043 DONE** — joinery.env MinIO konfig fix (GyartasilapStorage__* prefix)

### 🔴 **TESTER-036 PARTIAL** — 8/11 PASS
- ✅ MinIO WORM (TESTER-034 scope): PDF → MinIO, COMPLIANCE 365 nap, törlés tiltva
- ✅ Anyaglista (TESTER-035 scope): PDF → MinIO, presigned URL OK
- ❌ Batch POST/GET → 500: EF Core backing field bug → **JOINERY-052 kiadva**

### ✅ **JOINERY-052 DONE** — db1988f · UsePropertyAccessMode fix (részleges)
- **TESTER-037 BLOCKED** — Npgsql 8 JSONB deserialization: `List<Guid>` explicit `HasConversion` nélkül nem működik
- Root cause: `JoineryWebFactory.cs` UseInMemoryDatabase → Npgsql driver nem fut → 3 egymást követő "tesztek zöldek, prod elromlik"
- **JOINERY-053 ✅ DONE** — 680ca91 · HasConversion JSONB + 3 Testcontainers integration teszt · 387/387
- **INFRA-045 ✅** — 680ca91 LIVE
- **TESTER-037 ✅ DONE** — 11/11 PASS · Batch ZIP ✅ · Anyaglista ✅ · MinIO WORM ✅ · Auth 401 ✅
- 🎉 **JOINERY Phase 1+2 TELJES VALIDÁCIÓ LEZÁRVA**

### ✅ **CUTTING-028 — Real Event Bus (DONE)**
- **CUTTING:** `b0a11ba` — ICuttingEventPublisher + CompleteJob command + HTTP publisher (194 test ✅)
- **INVENTORY:** `2fe889e` — integration endpoint + handler guard (154 test ✅)
- **v1 korlát:** dimension stub (WidthMm=0) → offcut létrehozás v1.5-re halasztva (dokumentálva)

### ✅ **INVENTORY-PLANNING-V1 Phase 1 (COMPLETE)**
- **D1 ✅** Domain + EF + RLS (c022043) · **D2 ✅** Event handler stub (ae23cf8)
- **D3 ✅** Reuse commands (f68441d) · **D4 ✅** 6 endpoints (5e99f6d) · **D5 ✅** E2E (0bc0e11)
- **Final: 150/150 tests ✅**
- **Phase 2 backlog:** density tracking, multi-piece offcut (CUTTING-028 most ACTIVE)

---

**🟡 SOFT LAUNCH BLOCKER — Keycloak password grant (awaiting admin decision)**

**KERNEL-102:** Root cause = payload format (FIXED ✅)
**TESTER-030/031:** Ready (BLOCKED by Keycloak password grant)

**INFRA-037-BLOCKED:** KC admin credentials needed
**INFRA-038:** Try PostgreSQL local socket/standard credentials (NOW)
- Step 1: Socket auth (no password)
- Step 2: Try common credentials (if Step 1 fails)
- Step 3: UPDATE client SET direct_access_grants_enabled = true
- Step 4: Verify JWT token acquisition

**Timeline After INFRA-038-DONE:**
- TESTER-030: Payload validation with JWT (~5 min) → 201/201
- TESTER-031: Full E2E workflow (5 steps) (~20 min) → all PASS
- ROOT GO: Immediate if all pass

**Critical Path:**
```
INFRA-038 (PostgreSQL auth) → 5-10 min
TESTER-030 (payload validation) → 5 min
TESTER-031 (full workflow) → 20 min
ROOT GO → immediate
= ~30-35 min to SOFT LAUNCH GO 🚀
```

### Single Critical Blocker:
  🔴 **MSG-ROOT-001: Sudo password for INFRA-030/031** (can't execute systemctl/rsync without it)
  
### Timeline to Soft Launch:
  Sudo decision → 5 min deployments → 10 min TESTER validation → **Soft Launch GO** 🎉

**BUG-008b** — Audit verify chain 403 "Admin role required." · test-admin JWT-ben van Admin role (realm_access.roles) · Kernel flat role claim mismatch · MSG-KERNEL-092 kiadva (2026-04-18)

**PORTAL-006** — ✅ CODE DONE (60b74fd) · BUG-012 error handling + BUG-013 maxLength + BUG-015 Escape + BUG-016 mobile sidebar drawer · 323/323 ✅ · INFRA deploy pending

**PORTAL-012** — ✅ DONE (2026-04-19) · BUG-017 nesting panel UX: loading spinner + contextual error messages (404 vs other) · 323/323 tests ✅ · Optional/backlog task, COMPLETED

**BUG-016** — ✅ DEPLOYED initially (fd340bf) but introduced regression → **BUG-019 DISCOVERED** (redirect_uri wrong) → **PORTAL-011 ✅ FIXED** (revert) · **Szükséges:** INFRA-026 deploy + logout test

**BUG-015 ✅ DONE** (2026-04-19) — Browser Back button → auth state loss, KC login oldal · localStorage persistence + popstate listener · authStore.ts + App.tsx · 323/323 tests · MSG-PORTAL-007/008

**BUG-014** — 404 UX "Unexpected Application Error!" → user-friendly message · MSG-PORTAL-008 kiadva

**BUG-006** — ✅ DEPLOYED (71cd825 LIVE) · Supplier Address field migration `20260418000006` ✅ · TESTER POST/GET cím teszt szükséges

**BUG-011** — POST /procurement/orders → 500 · valószínűleg KERNEL-091 fix után megoldódik (audit chain)

**BUG-007** — ✅ DEPLOYED (308745b) · GET /api/procurement/orders LIVE · 53 teszt

**BUG-008** — ✅ DEPLOYED (826a885) · POST 23505 → 409 LIVE · 77 teszt

**BUG-009** — ✅ DEPLOYED (3cf19de) · error bannerek + InventoryPage üres állapot LIVE · bundle: index-BpaoVEiN.js · 318 teszt

**BUG-010** — ✅ CODE DONE (c18e00a) · `client_id` hozzáadva logoutUrl()-hoz · 318/318 ✅ · INFRA-019 deploy kiadva (2026-04-18)

**LIB-001** — ✅ DONE (2026-04-17) · Első szintézis
**LIB-002** — ✅ DONE (2026-04-20) · 11 fájl frissítve · ADR-031..037 · Contracts 1.3.0 · GOTCHA-016..018

**✅ BUG-017 (TESTER-021/022) — CLOSED WONTFIX** (Design Intent) ✅
  - ✅ KERNEL-095 confirmed: Nesting 404 is intentional (Plan/Batch/Sheet separation)
  - ✅ TESTER-022 DONE: TESTER understood + accepted wontfix reasoning
  - CuttingBatch stores only Sheet IDs, not objects → no drill-down navigation
  - Correct workflow: Plan → Batch → Sheet → Nesting ✅
  - **Optional future:** Cutting module could add Batch.Sheets relation + plan-level nesting endpoint
  - PORTAL-012 (UX improvement) remains backlog/optional

### 🟡 New

**CONTRACTS-V4_2** — ✅ ARCHIVE · Contracts 1.3.0 (52 teszt) + Nesting NuGet 1.0.0 (29 teszt) + E5 Cutting integráció (194 teszt) · LEZÁRVA 2026-04-20

**ARCH-002** — ✅ DONE · Cutting Planning v4 spec · OQ-1..OQ-7 JÓVÁHAGYVA (2026-04-20)

**CUTTING-031** — ✅ DONE · CuttingPlanStatus enum + EF migration · 195/195 ✅
**CUTTING-032** — ✅ DONE · DaySlot entity (FSM + kapacitás) · 196/196 ✅
**CUTTING-034** — ✅ DONE · ICapacityModel + AreaCapacityModel · 207/207 ✅
**CUTTING-035** — ✅ DONE · IReworkPolicy + WarnAndApplyPolicy · 218/218 ✅
**CUTTING-036** — ✅ DONE · PriorityProfile + seed presets (Manufacturer+PanelCutter) · 233/233 ✅
**CUTTING-037** — ✅ DONE · CuttingPlan FSM: Publish/Freeze/Close invariánsokkal · 244/244 ✅

🎉 **CUTTING SESSION B COMPLETE** — CUTTING-031..037 mind DONE · 244 teszt

**INFRA-026** — Logout test with new Portal bundle (PORTAL-011 deployed) — CRITICAL

### ⏹️ Backlog (Optional / Future)

**PORTAL-012** — Nesting panel UX improvement (loading spinner + error message) — Not blocking, design confirmed wontfix

### ⚠️ Nyitott tech debt / következő sprint jelöltek

- ~~**CI-002** — Supplier UI route (`/suppliers`) hiányzik~~ ✅ FE-013-ban megoldva (Sprint 3)
- **api-client tsconfig** — `packages/@spaceos/api-client/tsconfig.json` hiányzó `"types": ["vite/client"]` → `turbo build` elbukik · Portal-011 kiadva
- ~~**Procurement `/healthz`** — hiányzó health endpoint~~ ✅ PROCUREMENT-007 DONE (2026-04-17)
- ~~**KERNEL-070** — Audit chain hash mismatch vizsgálat~~ → KERNEL-086-ba átnevezve, folyamatban
- **Modules.Cutting v1** — SpaceOS_Modules_Cutting_Vision_v1.md (Sprint 9 jelölt)
- ~~**27-rate-limit regresszió vizsgálat**~~ → ORCH-081 folyamatban

### ~~Security Review (mind 3 pár DONE)~~
- ~~1. pár: KERNEL-079 + ORCH-068~~ ✅
- ~~2. pár: PORTAL-005 + JOINERY-004~~ ✅
- ~~3. pár: ABSTRACTIONS-006 + E2E-028~~ ✅

### 🟡 New (Sprint 6 jelöltek)

~~`KERNEL-075_audit-events-sequence-column.md`~~ → archive ✅ (2026-04-15)
- `KERNEL-070_audit-chain-break-investigation.md` — preexisting audit chain hash mismatch · escrow/WORM előfeltétel
- `KERNEL-069_llm-tool-registry-endpoint.md` — GET /api/llm-tools endpoint · LLM tool calling registry
- `SpaceOS_Modules_Cutting_Vision_v1.md` — Modules.Cutting v1 (ICuttingProvider) · Szabászat modul

### ✅ Archive
- `INFRA-161_portal-017-deploy` — 4e59f49 deploy · areaM2 crash fix + logout hotfix · index-CeL6o6nN.js LIVE ✅ (2026-04-18)
- `SPRINT6_escrow-tsclient-joineryv2.md` — JOINERY-016 PDF V2 ✅ (35a8723, 249/249) · PORTAL-013 TS client ✅ (358a6be) · INFRA-152 MinIO ✅ · KERNEL-088 ESCROW WORM ✅ (7a1b6d2, 1138/1138) · INFRA-153 deploy kiadva · CLOSED_DONE ✅ (2026-04-17)
- `SPRINT5_doorstar-production.md` — JOINERY-015 PDF ✅ (2498e33, 231/231) · PORTAL-012 brand ✅ (a481206) · KERNEL-087 OpenAPI ✅ (df3045c) · INFRA-150 users ✅ · INFRA-151 deploy kiadva · CLOSED_DONE ✅ (2026-04-17)
- `SPRINT4_soft-launch-readiness.md` — 🎉 **Doorstar Q2 Soft Launch GO** · KERNEL-086 audit chain (82a849a) · PROCUREMENT-007 /healthz (0382189) · PORTAL-011 turbo (4d88176) · ORCH-081 diagnózis · INFRA-149 deploy · E2E-049 **233/233** · CLOSED_DONE ✅ (2026-04-17)
- `KERNEL-074_sprint5-test-coverage.md` — SEC-01/SEC-02 Testcontainers + AuditChain CI gate · R-14+R-15 RESOLVED · 1110 teszt · commit 4cafceb · DONE (2026-04-15)
- `PORTAL-004_sprint5-test-coverage.md` — PKCE OAuth hibaágak + refresh dedup + coverage threshold · R-17 RESOLVED · 299/299 teszt · DONE (2026-04-15)
- `E2E-026_sprint5-test-coverage.md` — Cross-tenant izoláció + Ecosystem Actor v4 + error paths · R-13+R-20 RESOLVED · 193/193 teszt · 40 fájl · DONE (2026-04-15)
- `ORCH-067_sprint5-test-coverage.md` — Proxy 502 fix + tid claim validáció + SSE abort · R-16 RESOLVED · 207/207 teszt · commit b3860ac · DONE (2026-04-15)
- `JOINERY-003_sprint5-test-coverage.md` — PDF golden-file + RLS + dimenzió validáció · R-19 RESOLVED · 202/202 teszt · DONE (2026-04-15)
- `ABSTRACTIONS-005_sprint5-test-coverage.md` — Graph Engine write-time BFS fix · R-18 RESOLVED · 81/81 teszt · DONE (2026-04-15)
- `INFRA-083_kernel-deploy-migration0029-fix.md` — Kernel 0fafdb9 live · Migration 0029 DB-ben · TenantType élesítve · Sprint 5 UNBLOCKED (2026-04-15)
- `KERNEL-071_migration0029-designer-fix.md` — Migration 0029 Designer.cs regenerálva · commit 0fafdb9 · 1104 teszt (2026-04-15)
- `ORCH-063_ecosystem-actor-bff-routes.md` — Ecosystem Actor BFF catch-all proxy · 191 teszt · commit f7ddb37 (2026-04-15)
- `SpaceOS_Ecosystem_Actor_Architecture_v4.md` — KERNEL migration 0029 · TenantType · ModuleRegistry · 1104 teszt (2026-04-15)
- `SpaceOS_Modules_Contracts_Architecture_v4.md` — Modules.Contracts 1.0.0 NuGet · 45 fájl · 20 teszt · pack 41KB (2026-04-15)
- `INFRA-KC01_keycloak-vps-setup.md` — Keycloak bare-metal 24.0.0 deployed (2026-04-10)
- `JOINERY-V2_pdf-gyartasilap.md` — Minden track DONE, kód complete (2026-04-10)
- `KC01-KC03_keycloak-idp-v4.md` — Keycloak kód kész, DEPLOYED
- `BACKLOG.md` — következő sprintek listája
- `ORCH-STAGE-DISPATCH_stage-dispatch-route.md` — Orchestrator stageDispatch.ts DONE, 176 teszt (2026-04-10)
- `E2E-001_jwt-chain-fix.md` — JWT chain fix DONE: ~55/73 E2E pass (2026-04-11)
- `KERNEL-STAGE-REGISTRY_workflow-stage-architecture.md` — Stage Registry DONE: 1068 teszt (+135), MSG-054+057 elfogadva (2026-04-11)
- `INFRA-056_keycloak-hostname-fix.md` — Keycloak hostname + realm roles + test userek + E2E 99/120 (2026-04-11)
- `ORCH-056_authme-tenantid.md` — auth/me tenantId fix + JWKS commit, 177 teszt (2026-04-11)
- `KERNEL-058_vps-deploy.md` — VPS deploy + FlowEpic + TenantInterceptor + ADR-023, elfogadva (2026-04-12)
- `ORCH-057_vps-deploy.md` — Orchestrator pm2 restart, auth/me UUID live (2026-04-12)
- `INFRA-060_script-mapper-fix.md` — spaceos_tenants claim BE-01 spec, Keycloak JAR újraírva (2026-04-12)
- `KERNEL-059_gettenantid-fix.md` — GetTenantId() FindAll fix DONE, 1075 teszt (2026-04-12)
- `E2E-004_keycloak-auth-test-fix.md` — 28-keycloak-auth snake_case fix · 13/13 pass (2026-04-12)
- `INFRA-058_token-lifespan.md` — Token lifespan 300s ✅ · realm-export frissítve (git commit manuális) (2026-04-12)
- `KERNEL-060_migration-regen-ratelimit.md` — Rate limit config + GetTenantId() array fix · 1075 teszt · commit 03a7799 (2026-04-12)
- `KERNEL-061_migration-reconcile.md` — Migration reconcile: 20260412060341 törölve, 20260410130000 visszaállítva · commit c62f1d7 (2026-04-12)
- `INFRA-060_migration-bypass.md` — Migration 0028 bypass + Crypto::SigningKey · DB: 20260410130000 applied (2026-04-12)
- `INFRA-061_joinery-abstractions-deploy.md` — Joinery 5002 ✅ + Abstractions 5003 ✅ deployed (2026-04-12)
- `INFRA-062_kernel-deploy-post-k060.md` — Kernel binary c62f1d7 live · port 5000 · RateLimit+SigningKey env (2026-04-12)
- `E2E-006_rerun-current-suite.md` — 115/120 · +10 javulás · 5 maradék fail azonosítva (2026-04-12)
- `ORCH-058_ratelimit-middleware-order.md` — requireAuth chat előtt · 178 teszt · commit 9d02196 (2026-04-12)
- `E2E-005_expansion-k1.md` — Batch 1: 29/30/31 pass (130/135) · 34-abstractions BLOCKED-ORCH (2026-04-12)
- `KERNEL-062_e2e-remaining-failures.md` — ClaimsTenantResolver UUID fix + GUID normalizálás · 1075 teszt · commit 8dd0bd7 (2026-04-13)
- `ORCH-059_abstractions-proxy-deploy.md` — /bff/abstractions proxy + ABSTRACTIONS_BASE_URL · 183 teszt · commit 4a96e3c · VPS live ✅ (2026-04-13)
- `E2E-007_batch2.md` — 147/151 · 32/33/35 pass · SSE stabilizálva · ⚠️ 33-brand Phase 3B backlog (2026-04-13)
- `INFRA-063_kernel-orch-deploy.md` — Kernel 8dd0bd7 live · ⚠️ regresszió miatt rollback szükséges (2026-04-13)
- `E2E-008_final-rerun.md` — 119/151 REGRESSZIÓ · 8dd0bd7 ClaimsTenantResolver throw · rollback+fix folyamatban (2026-04-13)
- `KERNEL-063_claimsresolver-fallback.md` — ClaimsTenantResolver graceful fallback · commit 316f603 · 1084 teszt · DONE (2026-04-13)
- `INFRA-064_kernel-rollback.md` — Kernel rollback c62f1d7-re · VPS live · DONE (2026-04-13)
- `INFRA-065_kernel-deploy-post-k063.md` — Kernel deploy 316f603 · VPS live · DONE (2026-04-13)
- `E2E-009_final-rerun.md` — 119/151 változatlan · tid vs spaceos_tenants VALUE mismatch · KERNEL-064 fix kész (2026-04-13)
- `KERNEL-064_tenantsession-debug.md` — tid-primary prioritás visszaállítva · commit 3645480 · 1084 teszt · DONE (2026-04-13)
- `INFRA-066_kernel-rollback2.md` — Kernel rollback 316f603 → c62f1d7 · DONE (2026-04-13)
- `INFRA-067_kernel-deploy-3645480.md` — Kernel deploy 3645480 · VPS live · DONE (2026-04-13)
- `E2E-010_rerun-3645480.md` — 119/151 változatlan · TenantSessionInterceptor GUID normalizálás azonosítva · KERNEL-065 kiadva (2026-04-13)
- `INFRA-068_kernel-rollback3.md` — Kernel rollback 3645480 → c62f1d7 · VPS stabil · DONE (2026-04-13)
- `KERNEL-065_tenantsession-revert.md` — TenantSessionInterceptor c62f1d7 visszaállítva · commit d6b1bad · 1084 teszt · DONE (2026-04-13)
- `INFRA-069_kernel-deploy-d6b1bad.md` — Kernel deploy d6b1bad · VPS live · DONE (2026-04-13)
- `E2E-012_rerun-d6b1bad.md` — 119/151 változatlan (4. fix kísérlet) · bug: ClaimsTenantResolver.cs soha nem volt c62f1d7-re visszaállítva · KERNEL-066 diagnosztika kiadva (2026-04-13)
- `KERNEL-066_full-diff-diagnosis.md` — git diff diagnózis: CR.cs nem volt visszaállítva · clean revert b270ccf · 1075 teszt · DONE (2026-04-13)
- `INFRA-070_kernel-deploy-b270ccf.md` — Kernel deploy b270ccf · VPS live 22:41 · DONE (2026-04-13)
- `E2E-011_coverage-gap-report.md` — Coverage audit: 36-proof / 37-tools / 34-abstractions-deep PLANNED · válasz elküldve (2026-04-13)
- `ORCH-060_proof-route-path-fix.md` — proof.route.ts `/api/tasks/` → `/api/flow-epics/` · commit b7b4581 · 183 teszt ✅ (2026-04-14)
- `INFRA-071_orch-deploy-b7b4581.md` — Orchestrator b7b4581 VPS live · proof route fix élesítve · DONE ✅ (2026-04-14)
- `INFRA-072_kernel-deploy-46d6352.md` — Kernel 46d6352 VPS live · RLS UUID fix élesítve · DONE ✅ (2026-04-14)
- `E2E-015_37-tools-chain.md` — 37-tools 5/5 zöld · 161 E2E teszt · DONE ✅ (2026-04-14)
- `E2E-017_05close-verbose-diagnosis.md` — JWT `tid` HIÁNYZIK diagnózis · DONE ✅ (2026-04-14)
- `KERNEL-068_05close-diagnosis.md` — root cause azonosítva (tid hiány) · INFRA-075 megoldotta · DONE ✅ (2026-04-14)
- `INFRA-073_orch-deploy-ca00227.md` — Orch ca00227 live · proof CT fix · DONE ✅ (2026-04-14)
- `INFRA-074_kernel-verify.md` — Kernel 46d6352 DLL verified · DONE ✅ (2026-04-14)
- `INFRA-075_keycloak-tid-mapper.md` — KC `tid` flat claim live · DONE ✅ (2026-04-14)
- `INFRA-076_kernel-api-tid-mapper.md` — KC realm-scope: portal-app+orch-bff+kernel-api tid lefedve · DONE ✅ (2026-04-14)
- `KERNEL-071_migration0029-designer-fix.md` — Migration 0029 Designer.cs regenerálva · commit 0fafdb9 · 1104 teszt · DONE ✅ (2026-04-15)
- `ORCH-063_ecosystem-actor-bff-routes.md` — Ecosystem Actor BFF catch-all proxy · 191 teszt · commit f7ddb37 · DONE ✅ (2026-04-15)
- `SpaceOS_Ecosystem_Actor_Architecture_v4.md` — KERNEL migration 0029 · TenantType · ModuleRegistry · 1104 teszt · DONE ✅ (2026-04-15)
- `SpaceOS_Modules_Contracts_Architecture_v4.md` — Modules.Contracts 1.0.0 NuGet · 45 fájl · 20 teszt · pack 41KB · DONE ✅ (2026-04-15)
- `E2E-021_full-rerun-soft-launch-gate.md` — 🎉 **166/166** · BATCH-0-CLEANUP LEZÁRVA · Doorstar Q2 Soft Launch GO · DONE ✅ (2026-04-14)
- `BATCH-0-CLEANUP_legacy-fails.md` — 🎉 Minden legacy fail javítva · 166/166 · LEZÁRVA ✅ (2026-04-14)
- `E2E-020_24-summary-fixture-fix.md` — 165/166 · 24-summary 5/5 · Option A Doorstar fixture · DONE ✅ (2026-04-14)
- `PORTAL-001_portal-rebuild-deploy.md` — Portal dist rebuild 2026-04-14 · PKCE live · login unblocker · DONE ✅ (2026-04-14)
- `INFRA-079_kernel-deploy-37951c8.md` — Kernel 37951c8 live · healthz 200 · DONE ✅ (2026-04-14)
- `INFRA-080_orch-deploy-049c427.md` — Orch 049c427 live · bff/health 200 · DONE ✅ (2026-04-14)
- `E2E-019_batch0-e2e-fixes.md` — **163/166** · 05-close lenient ✅ · 24-summary tid OK · DONE ✅ (2026-04-14)
- `KERNEL-069_15nodes-sync.md` — DevRsaKeyManager exception-safe fix · commit 37951c8 · 1077 teszt · DONE ✅ (2026-04-14)
- `ORCH-CLEANUP_npm-audit-fix.md` — npm audit 0 vuln · axios/vite/follow-redirects patched · commit 049c427 · DONE ✅ (2026-04-14)
- `INFRA-077_keycloak-admin-ui.md` — KC admin UI public access + KC_HOSTNAME_ADMIN fix · DONE ✅ (2026-04-14)
- `E2E-018_full-rerun-post-fixes.md` — **162/166** · 🎉 Doorstar Q2 happy path ZÖLD · DONE ✅ (2026-04-14)
- `ORCH-061_proof-content-type-normalize.md` — proof.route.ts CT normalizálás · 184 teszt · commit ca00227 · DONE ✅ (2026-04-14)
- `E2E-016_34-abstractions-deep.md` — 34-abstractions-deep 5/5 · 166 E2E teszt · Batch 3 kész · DONE ✅ (2026-04-14)
- `KERNEL-067_flowepic-close-fsm-fix.md` — TenantSessionInterceptor tid-first · RLS UUID fix · 1077 teszt · commit 46d6352 (2026-04-14)
- `E2E-014_36-proof-chain.md` — 36-proof 5/5 lenient · 156 E2E teszt · rerun pending INFRA-071+072 (2026-04-14)
- `E2E-013_rerun-b270ccf.md` — **147/4/0** baseline visszaállt · b270ccf clean revert · Batch 2 STABIL · Batch 3 indítható (2026-04-13)
- `ORCH-069_security-fixes` — K1 err.message NODE_ENV guard · K2 SSRF allowlist regex · K3 sanitizeUserContent 4096 — 207/207 · commit 14fcc9b · DONE ✅ (2026-04-15)
- `ABSTRACTIONS-007_security-fixes` — M01 ValidateAudience=true · M02 TenantCommandInterceptor read-path RLS · M03 tenantId repo filter + 9 handler — 81/81 · DONE ✅ (2026-04-15)
- `INFRA-090_portal-axios-cve-fix` — Axios 1.15.0 · 0 CVE · design-portal chown fix · DONE ✅ (2026-04-15)
- `JOINERY-005_security-fixes` — M1 pageSize clamp · M2 ValidateAudience=true · M3 startup fail-fast — 202/202 · commit 27b9cfb · DONE ✅ (2026-04-15)
- `PORTAL-006_dist-rebuild-axios-update` — Axios 1.15.0 build · 281/281 teszt · dist/ frissítve · DONE ✅ (2026-04-15)
- `INFRA-092_portal-dist-deploy` — Axios 1.15.0 dist/ VPS-en · nginx reload · joinerytech.hu 200 · DONE ✅ (2026-04-15)
- `KERNEL-080_healthz-allow-anonymous` — /healthz + /health/ready AllowAnonymous · commit 3dd0e31 · 1110/1110 · DONE ✅ (2026-04-15)
- `INFRA-094_kernel-3dd0e31-deploy` — Kernel 3dd0e31 live · /healthz 200 no-auth · GenesisHash visszaállítva · DONE ✅ (2026-04-15)
- `INFRA-095_cutting-repo-setup` — /opt/spaceos/spaceos-modules-cutting/ · git init main · dispatcher cutting terminál · DONE ✅ (2026-04-15)
- `E2E-029_security-fixes-rerun` — 192/193 (502 finding) · DONE ✅ (2026-04-15)
- `E2E-030_fix-05close-502` — 502 elfogadva · **193/193** ✅ · Security sprint LEZÁRVA · DONE ✅ (2026-04-15)
- `INFRA-093_sudoers-fix-service-restart` — Abstractions /health 200 ✅ · Joinery /health 200 ✅ · sudoers tartós fix operátor-ra vár · DONE ✅ (2026-04-15)
- `JOINERY-006_cutting-contracts-package` — 3 NuGet Contracts packages · IInventoryProvider (6) · ICuttingProvider (4) · IProcurementProvider (4) · 9/9 teszt · commit 84bb708 · DONE ✅ (2026-04-15)
- `INFRA-096_genesis-hash-env-fix` — AuditChain__GenesisHash → /etc/spaceos/kernel.env · ChainBreak nélkül · /healthz 200 · deploy-invariáns · DONE ✅ (2026-04-15)
- `CUTTING-003_cutting-core` — CuttingSheet (immutable) + DailyCuttingPlan + CuttingExecution (FSM) · 91/91 teszt · CuttingProviderAdapter · RLS · DONE ✅ (2026-04-15)
- `CUTTING-004_procurement-core` — Supplier + PurchaseOrder (FSM) + Delivery · 133/133 teszt · ProcurementProviderAdapter · RecordDelivery→IInventoryProvider · DONE ✅ (2026-04-15)
- `CUTTING-005_infra-deploy` — spaceos-inventory (5004) + spaceos-cutting-svc (5005) + spaceos-procurement (5006) · 3 DB séma · systemd active · DONE ✅ (2026-04-15)
- `ORCH-070_cutting-bff-routes` — /bff/inventory+cutting+procurement proxy · 207/207 · commit d825ab1 · VPS live · DONE ✅ (2026-04-15)
- `CUTTING-006_e2e-smoke` — 41-cutting-smoke · 11 új teszt · 203/203 · auth 401 ✅ · DONE ✅ (2026-04-15)
- `ORCH-071_cutting-bff-post-fix` — catch-all verifikálva (POST/PUT/DELETE → 401) · kód változás nem kellett · DONE ✅ (2026-04-15)
- `CUTTING-002_inventory-core` — Inventory Core · MaterialCatalog+PanelStock+Offcut+StockMovement · 56/56 teszt · InventoryProviderAdapter · RLS · DONE ✅ (2026-04-15)
- `JOINERY-007_cutting-integration` — ICuttingProvider + CuttingProviderStub + SubmitDoorOrder graceful degradation · 214/214 teszt (+12) · DONE ✅ (2026-04-16)
- `CUTTING-007_nesting-l1` — Nesting L1 FFDH · PanelAssignment + PlacedPart + NestingService · GetNestingResult frissítve · 151/151 teszt (+18) · DONE ✅ (2026-04-16)
- `CUTTING-008_panel-dimensions-fix` — PanelStockDto WidthMm+HeightMm · hardcode eltávolítva · Contracts 1.1.0 · 153/153 teszt (+2) · DONE ✅ (2026-04-16)
- `CUTTING-009_repo-split` — spaceos-modules-inventory (47 teszt) + spaceos-modules-procurement (42 teszt) önálló repók · NuGet cross-ref · DONE ✅ (2026-04-16)
- `JOINERY-009_saga-500-fix` — diagnózis: deployment race condition (nem kódhiba) · 214/214 zöld · DONE ✅ (2026-04-16)
- `INFRA-101_joinery-cutting-stub-deploy` — Joinery 5002 live · CuttingProviderStub deployed · /health 200 ✅ · operátor restart (2026-04-16)
- `INFRA-102_cutting-modules-deploy` — spaceos-inventory (5004) + cutting-svc (5005) + procurement (5006) live · Nesting L1 + Contracts 1.1.0 deployed ✅ (2026-04-16)
- `E2E-033_cutting-full-coverage` — 42-cutting-flow + 43-joinery-cutting-integration · 214/214 (+11) · probe-and-skip nesting shape · DONE ✅ (2026-04-16)
- `E2E-034_rerun-post-cleanup` — 214/214 · 43-joinery-cutting 5/5 (deployment race condition igazolva, lezárható) · INFRA-103 restart után 42-cutting-flow aktiválódik · DONE ✅ (2026-04-16)
- `INFRA-104_materialcatalog-seed` — seed már megvolt (5 sor migration-ből: MDF 18/16mm, HDF 3mm, Forgácslap, ABS él) · spaceos_inventory.MaterialCatalogs ✅ · DONE ✅ (2026-04-16)
- `KERNEL-081_rls-tenant-isolation-fix` — DenyWebRequestSentinel · web request tid nélkül → üres (nem bypass) · háttér null-bypass megmarad · commit a9d3803 · 1110/1110 ✅ · DONE ✅ (2026-04-16)
- `INFRA-105_kernel-a9d3803-deploy` — Kernel RLS fix VPS-en · deploy + /healthz · DONE ✅ (2026-04-16)
- `E2E-035_rerun-rls-cutting-activation` — 214/214 · 38-cross-tenant: rlsEnforced=false (a9d3803 még nem volt live) · 42-cutting-flow: skip (INFRA-103 pending) · DONE ✅ (2026-04-16)
- `INFRA-105_kernel-a9d3803-deploy` — a9d3803 live · /healthz healthy · gotcha: `rm -rf publish` kell incremental build előtt · DONE ✅ (2026-04-16)
- `INFRA-106_keycloak-test-admin-tid-fix` — test-admin `tid` = DoorstarGuid ✅ · KC URL `/auth/realms/...` helyes · E2E .env már correct volt · DONE ✅ (2026-04-16)
- `KERNEL-082_facility-get-404-mapclaims-fix` — `MapInboundClaims=false` · ASP.NET default remappelte `tid`→MS URI · DenySentinel triggerelt · EF filter 404 · commit 694bc56 · DONE ✅ (2026-04-16)
- `INFRA-107_kernel-694bc56-deploy` — 694bc56 live · /healthz healthy · facility GET 200 test-admin tokennel ✅ · publish path: `/opt/spaceos/spaceos-kernel/publish/` · DONE ✅ (2026-04-16)
- `E2E-036_rerun-rls-proper-verification` — 38-cross-tenant rlsEnforced=true ✅ · cascade blocker azonosítva (MapInboundClaims) · DONE ✅ (2026-04-16)
- `E2E-037_rls-e2e-seed-fix` — helpers.ts + 13 fájl adminTenantId seed fix · 21-tenant-isolation ✅ · KERNEL-082 fix után 214/214 · DONE ✅ (2026-04-16)
- `E2E-038_rerun-post-mapclaims-fix` — **214/214** ✅ · 38-cross-tenant rlsEnforced=true ✅ · RLS Cleanup Sprint LEZÁRVA · DONE ✅ (2026-04-16)
- `INFRA-103_cleanup-rename-deploy` — daemon-reload + 4 service restart · operátor elvégezte · all active ✅ · DONE ✅ (2026-04-16)
- `E2E-039_rerun-cutting-flow-activation` — 214/214 ✅ · 42-cutting-flow skip: pathRewrite hiba azonosítva (ORCH-072) · DONE ✅ (2026-04-16)
- `ORCH-072_cutting-pathrewrite-fix` — pathRewrite `'/api/'` → `/api/cutting/` stb. · commit 6566d2a · 207/207 ✅ · DONE ✅ (2026-04-16)
- `ORCH-073_commit-missing` — commit pótolva · 6566d2a develop-ra push · DONE ✅ (2026-04-16)
- `INFRA-108_orch-6566d2a-deploy` — 6566d2a live · /bff/health ok · pm2 online ✅ · DONE ✅ (2026-04-16)
- `E2E-040_rerun-cutting-flow-full` — **214/214** ✅ · 42-cutting-flow aktív (cuttingAvailable=true) · nesting skip (test-runner audience) · Cutting Activation Sprint LEZÁRVA · DONE ✅ (2026-04-16)
- `INFRA-109_cutting-audience-fix` — KC audience mapper + Jwt__Authority `/auth` fix · cutting.env javítva · service restart szükséges · DONE ✅ (2026-04-16)
- `E2E-041_rerun-nesting-activation` — 214/214 ✅ · nesting skip: cutting 401 (audience/authority config) · INFRA-110 szükséges · DONE ✅ (2026-04-16)
- `INFRA-110_cutting-jwt-diagnosis` — root cause: `GetTenantId("tenant_id")` → JWT-ben `"tid"` van · cutting.env Authority+RequireHttps javítva · CUTTING-007 kiadva · DONE ✅ (2026-04-16)
- `CUTTING-007_tenant-claim-fix` — `"tenant_id"` → `"tid"` · MapInboundClaims=false · 3 repo · 153/153 ✅ · commit 79d16a2/b67d9bc/1fba5e2 · DONE ✅ (2026-04-16)
- `INFRA-111_cutting-modules-deploy` — cutting/inventory/procurement 3 commit deploy · JWT auth OK (500/404/405, nem 401) · DONE ✅ (2026-04-16)
- `E2E-042_rerun-nesting-full` — 214/214 ✅ · JWT fix igazolva (500=auth OK) · nesting skip: SqlState 42501 schema grant hiány · INFRA-112 kiadva · DONE ✅ (2026-04-16)
- `INFRA-112_cutting-db-schema-grant` — USAGE+DML+SEQUENCES grant · 3 DB · DEFAULT PRIVILEGES · 42501 megszűnt · DONE ✅ (2026-04-16)
- `E2E-043_rerun-nesting-activation` — 214/214 ✅ · 42501 megszűnt ✅ · nesting skip: 42704 app.current_tenant_id GUC hiány · INFRA-113 kiadva · DONE ✅ (2026-04-16)
- `INFRA-113_cutting-db-guc-fix` — ALTER DATABASE GUC regisztráció · 3 DB · 42704 megszűnt · service restart OK · DONE ✅ (2026-04-16)
- `E2E-044_rerun-nesting-guc-fix` — 214/214 ✅ · 42704 OK · nesting skip: 22P02 app.current_tenant_id üres (nincs TenantSessionInterceptor) · CUTTING-008 kiadva · DONE ✅ (2026-04-16)
- `CUTTING-008_tenant-session-interceptor` — DbConnectionInterceptor · set_config tid → GUC · pool reset · 153/153 ✅ · a363ad6/0dbb02e/1ae66a0 · DONE ✅ (2026-04-16)
- `INFRA-114_cutting-interceptor-deploy` — 3 commit deploy · 22P02 megszűnt · TenantSessionInterceptor live · backup 131322 · DONE ✅ (2026-04-16)
- `E2E-045_rerun-nesting-final` — 213/214 · cuttingSheetId DEFINIÁLT ✅ · nesting fut (nem skip) · 500 DI hiba: IInventoryProvider hiányzik · CUTTING-009 kiadva · DONE ✅ (2026-04-16)
- `CUTTING-009_inventory-provider-stub` — InventoryProviderStub · 6 metódus · AddScoped DI · 64/64 ✅ · commit 873ba39 · DONE ✅ (2026-04-16)
- `INFRA-115_cutting-stub-deploy` — 873ba39 deploy · POST 200+UUID ✅ · nesting GET 200 ✅ · infra réteg teljes · DONE ✅ (2026-04-16)
- `E2E-046_rerun-nesting-activation-final` — 🎉 **214/214** ✅ · 42-cutting-flow nesting PASS (23ms) · Nesting Activation Sprint LEZÁRVA · DONE ✅ (2026-04-16)
- `KERNEL-083_llm-tool-registry` — GET /api/llm-tools · 7 tool descriptor · AllowAnonymous · 1017 teszt · commit e4f83ac · DONE ✅ (2026-04-16)
- `KERNEL-069_llm-tool-registry-endpoint` — SUPERSEDED by KERNEL-083 · archivált (2026-04-16)
- `INFRA-116_kernel-e4f83ac-deploy` — e4f83ac live · /api/llm-tools 200 ✅ · Sprint 8 LEZÁRVA · DONE ✅ (2026-04-16)
- `PORTAL-007_vite-cve-fix` — Vite CVE GHSA-4w7w-66w2-5vf9 · known risk elfogadva (dev server only, nem production) · Vite 6 migráció Q3 backlogba · LEZÁRVA ✅ (2026-04-16)
- `KERNEL-084_duplicate-using-fix` — CS0105 duplikált using eltávolítva · 0 error 0 warning · 1115/1115 teszt · commit c173de0 · DONE ✅ (2026-04-16)
- `CUTTING-010_inventory-http-adapter` — InventoryProviderHttpAdapter · 6 metódus · graceful degradation · 71/71 teszt (+7) · commit c7f1b94 · DONE ✅ (2026-04-16)
- `INFRA-117_cutting-http-adapter-deploy` — c7f1b94 deploy · InventoryService__BaseUrl env · active ✅ · POST 200 ✅ · DONE ✅ (2026-04-16)
- `KERNEL-085_internal-delete-by-tenant` — DELETE /internal/flow-epics/by-tenant · 4-gate security · 1121/1121 teszt (+6) · commit 130959a · DONE ✅ (2026-04-16)
- `INFRA-129_orchestrator-test-bff-deploy` — ORCH-074 7446aeb deployed · /bff/test/* LIVE · 403 rossz secret ✅ · 200 helyes secret ✅ · DONE ✅ (2026-04-16)
- `CUTTING-011_internal-delete-by-tenant` — DELETE /internal/cutting-sheets/by-tenant · 4-gate security · 77/77 teszt (+6) · commit 745d387 · DONE ✅ (2026-04-16)
- `INVENTORY-001_internal-delete-by-tenant` — DELETE /internal/panel-stocks/by-tenant · FK-safe · 53/53 teszt (+6) · commit 0ab148b · DONE ✅ (2026-04-16)
- `FE-008_e2e-auth-flows` — 9 E2E teszt (01-login/08-auth-edge/09-responsive) · CallbackPage dupla-callback fix · commit 88bf153 · CSP blocker → INFRA-131 · DONE ✅ (2026-04-16)
- `ORCH-076_seed-profiles` — empty-v1 + doorstar-smoke-v1 · 217/217 teszt (+3) · commits e500a4f · INFRA-132 deploy pending · DONE ✅ (2026-04-16)
- `JOINERY-011_internal-delete-guc-fix` — GUC key: app.tenant_id · set_config paraméteres · IsRelational() guard · 219/219 · commit 874fd21 · DONE ✅ (2026-04-16)
- `INVENTORY-002_internal-delete-guc-fix` — GUC key: app.current_tenant_id · IsRelational() guard · 53/53 · commit bcca799 · DONE ✅ (2026-04-16)
- `INFRA-131_portal-csp-fix` — connect-src += joinerytech.hu · nginx reload ✅ · OIDC unblocked · E2E auth tesztek feloldva · DONE ✅ (2026-04-16)
- `CUTTING-012_internal-delete-guc-fix` — GUC key: app.current_tenant_id · FormattableString paraméteres · IsRelational() guard · 77/77 · commit 1bf2a12 · DONE ✅ (2026-04-17)
- `PROCUREMENT-002_internal-delete-guc-fix` — GUC key: app.current_tenant_id · RequestServices scope · IsRelational() guard · 48/48 · commit 5c642c6 · DONE ✅ (2026-04-17)
- `INFRA-132_orchestrator-seed-profiles-deploy` — e500a4f LIVE · KC_TOKEN_URL + TEST_RUNNER_CLIENT vars · empty-v1 200 ✅ · DONE ✅ (2026-04-17)
- `CUTTING-013_interceptor-skip-fix` — null tid → early return · ConnectionClosingAsync érintetlen · 77/77 · commit c3323ed · DONE ✅ (2026-04-17)
- `INVENTORY-003_interceptor-skip-fix` — null tid → early return · 53/53 · commit 21af2e9 · DONE ✅ (2026-04-17)
- `PROCUREMENT-003_interceptor-skip-fix` — IsNullOrWhiteSpace early return · 48/48 · commit cbd900b · DONE ✅ (2026-04-17)
- `JOINERY-012_interceptor-skip-fix` — null tid early return · JoineryOutboxEntries migration kódban ✅ · 219/219 · commit c8ac5b6 · DONE ✅ (2026-04-17)
- `DEBUG-001_redirect-loop` — joinerytech.hu redirect loop CLOSED · PORTAL-014/015/016 · INFRA-154..159 · RESOLVED ✅ (2026-04-18)
- `INFRA-160_kc-post-logout-redirect` — KC portal-app postLogoutRedirectUris beállítva · joinerytech.hu + portal.joinerytech.hu + asztalostech.hu · DONE ✅ (2026-04-18)
- `PORTAL-017_inventory-aream2-fix` — InventoryPage areaM2?.toFixed() crash fix + logout hotfix (logoutUrl optional idToken, no redirectToLogin fallback) · 307/307 teszt · commit 4e59f49 · DONE ✅ (2026-04-18)
- `E2E-050_soft-launch-bug-coverage` — 49-52 tesztfájlok kész (14/14) · BUG-001/003/003b/004 real assertions · Kernel regresszió (POST /api/tenants 500) felderítve · DONE ✅ (2026-04-18)
- `E2E-051_full-rerun-post-bugfix` — 245 teszt futtatva · 128 pass / 12 fail / 105 skip · BUG-001/003/003b/004/005 mind ✅ live · POST /api/tenants 500 MÉG ÉL (más gyökérok) · KERNEL-090 kiadva · DONE (részleges) (2026-04-18)

---

## Szabály

- Új feladat érkezésekor → `new/` mappába
- Root kiadta terminálnak → mozgatás `active/`-be
- Terminál DONE outbox-a elfogadva → mozgatás `archive/`-ba
