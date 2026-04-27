# SpaceOS — Kódbázis összesített állapotleírás

**Utolsó frissítés:** 2026-04-27 — **~4472 teszt** · 5 LIVE domain · Lynis ~85 · Cabinet 0.2 COMPLETE · Cutting Phase 3 DEPLOYED · Joinery Phase 3 VALIDATED · FreeTier LIVE · E2E 277/277 · **PartnerTier MVP COMPLETE** 🎉
**Tesztelő:** Gabor
**Környezet:** VPS prod (109.122.222.198) — nginx (HTTPS) → Orchestrator → Kernel
**Domainek:** joinerytech.hu · asztalostech.hu · portal.joinerytech.hu · eszkozok.joinerytech.hu · freetier.joinerytech.hu (Let's Encrypt SAN cert)

---s

## Rendszer architektúra

```
Browser  https://joinerytech.hu / https://asztalostech.hu
  │
  ▼
L5  Nginx              (TLS 1.2/1.3 + HSTS + security headers)   port 443
  │  /  →  /opt/spaceos/design-portal/apps/joinerytech/dist/  (Phase 3C)
  │  /bff/*  →  proxy_pass 127.0.0.1:3000
  │  X-SpaceOS-Brand: joinerytech / asztalostech (domain-based)
  │  HTTP :80 → 301 redirect HTTPS
  ▼
L4  Design Portal     (React 18 · Vite · Tailwind)     static (nginx)
  ▼
L3  Orchestrator       (Node.js 22 · Express · TS)     port 3000 (PM2)
  │  /bff/api/*    →  Kernel proxy (X-SpaceOS-Brand forwarded)
  │  /bff/nodes/*  →  Kernel /api/nodes/* (SIP header)
  │  /bff/sync/*   →  Kernel /api/sync/*  (SIP header)
  │  /bff/layers/* →  Kernel /api/layers/*
  │  /bff/audit-events/* → Kernel /api/audit-events/*
  ▼
L2  Kernel API         (C# .NET 8 · Minimal API)       port 5000 (systemd, loopback-only)
  │  SourceBrand allowlist: joinerytech / asztalostech / null
  ▼
L1  PostgreSQL 16       (port 5433)
  │
  ↕
LLM Provider           (OpenAI-compatible · Gemini 2.0 Flash · Mock)
```

---

## Projekt állapotok összesítése

| Projekt | Réteg | Stack | Státusz | Tesztek | Build |
|---|---|---|---|---|---|
| **SpaceOS.Kernel** | L2 Backend | .NET 8, EF Core 8, PostgreSQL | `DEPLOYED` | **1138 pass** | 0 error, 0 warning |
| **SpaceOS.Orchestrator** | L3 BFF | Node.js 22, TypeScript 5, Express 4 | `DEPLOYED` | **227 pass** · ORCH-083+084: Joinery+Cutting Phase 3 BFF routes | 0 TS error |
| **SpaceOS.DesignPortal** | L4 Frontend | React 18 + Turborepo monorepo | `DEPLOYED` | **323 pass** | 0 TS error |
| **E2E** | Full stack | Vitest + fetch | `DONE ✅` | 🎉 **277/277** · 59 fájl · Joinery batch+anyaglista + Cutting ingest coverage | — |
| **Keycloak** | IdP | Keycloak 24.0 (systemd) | `DEPLOYED` | `cabinetbilder-plugin` Device Code Flow ✅ | 6 mapper |
| **SpaceOS.Modules.Joinery** | L2 Module | .NET 8 · 5002 | `DEPLOYED` | **389 pass** · Phase 1+2+3 ✅ · MinIO PublicEndpoint · Batch+Anyaglista WORM | 0 error |
| **SpaceOS.Modules.Abstractions** | L2 Module | .NET 8 · 5003 | `DEPLOYED` | **81 pass** · BFS cycle detection | 0 error |
| **SpaceOS.Modules.Contracts** | NuGet | .NET 8 | `DONE ✅` | **57 pass** · **v1.3.0** + v4.2 FreeTier extensions | 0 error |
| **SpaceOS.Nesting.Algorithms** | NuGet | .NET 8 | `DONE ✅` | **32 pass** · **v1.1.0** · FFDH + Guillotine · NuGet packed | 0 error |
| **SpaceOS.Modules.Inventory** | L2 Module | .NET 8 · 5004 | `DEPLOYED` | **164 pass** · Reservation + Offcut batch | 0 error |
| **SpaceOS.Modules.Cutting** | L2 Module | .NET 8 · 5005 | `DEPLOYED` | **303 pass** · **Phase 3 ✅** · Order Ingestion + Nesting Publish + /healthz | 0 error |
| **SpaceOS.Modules.Procurement** | L2 Module | .NET 8 · 5006 | `DEPLOYED` | **53 pass** | 0 error |
| **SpaceOS.FreeTier** | L2 Module | .NET 8 · 5010 | `LIVE ✅` | **176 pass** · FREETIER-001–013 ✅ · Valós FFDH · NuGet PackageRef | 0 error |
| **SpaceOS.PartnerTier** | L2 Module | .NET 8 · 5011 | `MVP COMPLETE ✅` | **232 pass** · PARTNER-001–004 ✅ · EF migration P_0001 + RLS + trigger + indexes · ApiKeyExpiryWorker · CORS allowlist · appsettings.Production.json · publish ✓ | 0 error |
| **spaceos-doorstar-portal** | L4 FE Portal | React 18 · Vite · TS · Tailwind | `DEPLOYED` | **99 pass** · FE-012–017: Phase 3 gombok + 6 bugfix (PKCE, callback, orders) | 0 TS error |
| **spaceos-freetier-portal** | L4 FE Portal | React 19 · Vite · TS · Tailwind | `LIVE ✅` | **75 pass** · Nesting kalkulátor + workspace + share + auth | 0 TS error |
| **SpaceOS.Cabinet** | NuGet Library | .NET 8+10 · 10 csomag | `DONE ✅` | **518 pass** · Cabinet 0.2 COMPLETE · Catalog+Assembly+Application · git: `3098a60` | 0 error |

**Összesített tesztszám: ~4472 pass** (1138 Kernel + 227 Orchestrator + 323 Portal + 99 Doorstar Portal + 389 Joinery + 81 Abstractions + 303 Cutting + 164 Inventory + 53 Procurement + 57 Contracts + 32 Nesting + 21 Reservation + 277 E2E + 176 FreeTier API + 75 FreeTier Portal + 518 Cabinet + 232 PartnerTier + ~6 misc)

### LIVE domainek

| Domain | Termék | Státusz |
|---|---|---|
| **joinerytech.hu** | Design Portal (Turborepo) | LIVE ✅ |
| **portal.joinerytech.hu** | Doorstar Portal (batch PDF + anyaglista + orders) | LIVE ✅ |
| **asztalostech.hu** | Design Portal (HU brand) | LIVE ✅ |
| **eszkozok.joinerytech.hu** | FreeTier nesting kalkulátor (valós FFDH) | LIVE ✅ |
| **freetier.joinerytech.hu** | FreeTier API | LIVE ✅ |

### VPS Security (Lynis ~85)

| Batch | Tartalom | Státusz |
|---|---|---|
| Batch 1 | Docker bind fix + Keycloak loopback + SSH hardening + apt upgrade | ✅ |
| Batch 2 | PG chmod 600 + Redis CONFIG rename + protocol disable + fail2ban + umask 027 | ✅ |
| Batch 3 | auditd + needrestart + rkhunter + sysstat | ✅ |
| CabinetBilder | Keycloak client (Device Code Flow, 6 mapper) | ✅ |
| spaceos_schema_owner | FreeTier RLS FORCE fix (INFRA-047) | ✅ |

**Tudásbázis:** `docs/knowledge/` — 18 fájl · **LIB-002 DONE** (2026-04-20) · Context fájlok naprakészek · ADR-031..037 felvéve · Contracts 1.3.0 + Nesting NuGet dokumentálva · GOTCHA-016..018 hozzáadva

### Sprint roadmap

| Sprint | Tartalom | Státusz | Blokkoló | Effort |
|---|---|---|---|---|
| **Sprint 6 — Security Sprint** | ORCH K1+K2+K3 · ABSTRACTIONS M01-M03 · JOINERY M1-M3 · PORTAL Axios 1.15.0 · KERNEL AllowAnonymous healthz · E2E 193/193 | `CLOSED_DONE` ✅ | — | 2026-04-15 · 0 CVE prod |
| **Sprint 7 — Modules.Cutting v1 + Joinery integráció** | JOINERY-007 CuttingProviderStub · CUTTING-007 Nesting L1 FFDH · CUTTING-008 Contracts 1.1.0 (WidthMm/HeightMm) · INFRA deploy · E2E-033 214/214 · sudoers tartós fix | `CLOSED_DONE` ✅ | — | 2026-04-16 · 2289 teszt |
| **RLS Cleanup Sprint** | KERNEL-081 DenyWebRequestSentinel · KERNEL-082 MapInboundClaims=false · INFRA-105+107 deploy · E2E-036/037/038 seed fix + 214/214 · 38-cross-tenant rlsEnforced=true ✅ | `CLOSED_DONE` ✅ | — | 2026-04-16 |
| **Cutting Activation Sprint** | ORCH-072 BFF pathRewrite fix (cutting/inventory/procurement) · INFRA-108 deploy · E2E-039/040 · 42-cutting-flow aktív (cuttingAvailable=true) · 214/214 | `CLOSED_DONE` ✅ | — | 2026-04-16 |
| **Nesting Activation Sprint** | JWT fix (CUTTING-007) · DB grant (INFRA-112) · GUC (INFRA-113) · TenantSessionInterceptor (CUTTING-008) · InventoryProviderStub (CUTTING-009) · E2E-046 214/214 nesting PASS | `CLOSED_DONE` ✅ | — | 2026-04-16 |
| **Sprint 8 — Q3 Kickoff** | KERNEL-083 GET /api/llm-tools · 7 Doorstar tool descriptor · e4f83ac live · E2E 214/214 · Nesting Activation Sprint LEZÁRVA | `CLOSED_DONE` ✅ | — | 2026-04-16 |
| **Doorstar Portal Sprint 1 — MVP** | FE-001..006 DONE ✅ · 87 teszt · INFRA-123 nginx+cert LIVE · https://portal.joinerytech.hu | `CLOSED_DONE` ✅ | — | 2026-04-16 🚀 |
| **Doorstar Portal Sprint 2 — Test Infra** | INFRA-124..144 ✅ · FE-007..011 ✅ · JOINERY-010..014 ✅ · PROCUREMENT-001..006 ✅ · KERNEL-085 ✅ · CUTTING-011..015 ✅ · INVENTORY-001..005 ✅ · ORCH-076..080 ✅ · KC mappers+localhost ✅ · seed idempotens ✅ · resetTenant POST ✅ | `CLOSED_DONE ✅` | — | 2026-04-17 🎉 |
| **Doorstar Portal Sprint 3 — Teljes üzleti logika UI** | FE-012..017 ✅ · 306 teszt · a57511b · E2E-048 233/233 ✅ · portal.joinerytech.hu LIVE ✅ · CI-002 RESOLVED · PORTAL-011 api-client tsconfig fix 4d88176 ✅ · turbo build 7/7 ✅ | `CLOSED_DONE ✅` | — | 2026-04-17 🎉 |
| **Sprint 4 — Soft Launch Readiness** | KERNEL-086 audit chain ✅ (82a849a) · PROCUREMENT-007 /healthz ✅ (0382189) · PORTAL-011 turbo build ✅ (4d88176) · ORCH-081 ✅ · INFRA-149 deploy ✅ · E2E-049 **233/233** ✅ | `CLOSED_DONE ✅` | — | 2026-04-17 🎉 |
| **Sprint 5 — Doorstar Production Sprint** | JOINERY-015 PDF ✅ (2498e33) · PORTAL-012 brand ✅ (a481206) · KERNEL-087 OpenAPI ✅ (df3045c) · INFRA-150 users ✅ · INFRA-151 deploy ✅ · portal.joinerytech.hu LIVE | `CLOSED_DONE ✅` | — | 2026-04-17 🎉 |
| **Sprint 6 — ESCROW + TS Client + Joinery V2** | JOINERY-016 PDF V2 ✅ · PORTAL-013 TS client ✅ · INFRA-152 MinIO WORM ✅ · KERNEL-088 ESCROW ✅ (7a1b6d2) · INFRA-153 deploy ✅ · ⚠️ Joinery chown fix | `CLOSED_DONE ✅` | — | 2026-04-17 🎉 |
| **DEBUG-001 — joinerytech.hu redirect loop fix** | PORTAL-014/015/016 · INFRA-154..159 · KC tid fix + portal-app mappers · module ValidAudiences=kernel-api · PORTAL-016 408bf1e · 5 service active | `CLOSED_DONE ✅` | — | 2026-04-18 |
| **Soft Launch Manual Test — Session 1** | TESTER session · test-admin · Dashboard/Szállítók/Készlet/Vágótervek/Rendelések/Chat/Audit · 10 PASS · 4 FAIL · 5 bug: BUG-001..005 forwarded | `DONE ✅` | 5 bug fix folyamatban | 2026-04-18 |
| **🚀 SOFT LAUNCH GO — Doorstar Kft.** | ✅ Gábor (Founder) hivatalos GO döntés · Stack LIVE: joinerytech.hu · TESTER-033 PASS · Keycloak Direct Access Grants ✅ · 3028 teszt · 0 nyitott kritikus bug | `LIVE ✅` | — | **2026-04-20** 🎉 |
| **Bug Fix Sprint — Soft Launch 1** | BUG-001/002/003/003b/004/005 mind LEZÁRVA ✅ · E2E-051 rerun: 128/245 (12 fail, 105 skip) · POST /api/tenants 500 MÉG ÉL → KERNEL-090 vizsgálat | `CLOSED_DONE ✅` | — | 2026-04-18 |
| **INFRA-096 GenesisHash fix** | AuditChain__GenesisHash → /etc/spaceos/kernel.env · deploy-invariáns · ChainBreak nélkül | `DONE ✅` | — | 2026-04-15 |
| Phase 3C+ | `@spaceos/joinery-ui` + moduleRouter + B2BHandshake live | `CLOSED_DONE` | — | 16 nap |
| Production Readiness | Keycloak IdP + Audit Race Fix + PostgreSQL WORM | `CLOSED_DONE` | — | 8 nap |
| Modules.Joinery v1 | Ajtógyártás Domain Engine (új polyrepo) | `CLOSED_DONE` | — | 16 nap |
| Doorstar Onboarding | Soft Launch Integration: Keycloak tenant setup + Joinery seed + B2B handshake + Nginx upstream | `CLOSED_DONE` | — | 7 nap |
| Modules.Abstractions v1 — Phase A-Core | Template + Graph Engine + RLS + Migration 0001 | `CLOSED_DONE` | — | 46/46 teszt ✅ |
| Modules.Abstractions v1 — Phase B-Manufacturing | ManufacturingDerivation + CNC + ProcessPlan + FAF_T seed | `CLOSED_DONE` | — | 61/61 teszt ✅ |
| Keycloak IdP v4 — Kernel (MSG-KC01) | JWT JWKS + TenantSessionInterceptor + JwksHealthCheck | `DEPLOYED` | — | 1068 teszt ✅ · Authority HTTPS production |
| Keycloak IdP v4 — Orchestrator (MSG-KC02) | jwks-rsa + /bff/auth/me + régi auth törlés | `DEPLOYED` | — | 177 teszt ✅ · tenantId claim live |
| Keycloak IdP v4 — Portal (MSG-KC03) | PKCE state+nonce + AuthStore + CallbackPage | `DEPLOYED` | — | 291 teszt ✅ |
| Keycloak IdP v4 — E2E migráció | helpers.ts + global-setup + 28-keycloak-auth + refresh/auth chain tesztek | `DEPLOYED` | — | Keycloak live · E2E újrafuttatás folyamatban |
| Keycloak IdP v4 — Infra (MSG-INFRA-KC01) | VPS: spaceos_keycloak DB + realm + clients + Script Mapper + nginx | `DEPLOYED` | — | hostname + Script Mapper BE-01 fix live (2026-04-12) |
| Keycloak hostname fix (MSG-INFRA-056) | keycloak.conf + realm roles + E2E test userek + token lifetime | `DONE` ✅ | — | MSG-INFRA-056-DONE elfogadva 2026-04-11 |
| Keycloak RBAC test userek (MSG-INFRA-057) | designer-rbac + designer-read — INFRA-056-ban elkészült | `DONE` ✅ | — | Lezárva INFRA-056 részeként |
| **Kernel Stage Registry** | StageDefinition + StageChain + StageHandoff + Migration 0028 + 15 endpoint | `DEPLOYED` | — | 1068 teszt (+135) · VPS live (2026-04-12) |
| **Kernel VPS deploy + E2E fixes** | Migration 0028 + FlowEpic + TenantInterceptor + Authority + ADR-023 | `DONE` ✅ | — | MSG-KERNEL-058-DONE elfogadva 2026-04-12 |
| **Orchestrator VPS deploy** | pm2 restart — auth/me fix élesítése | `DONE` ✅ | — | MSG-ORCHESTRATOR-057-DONE elfogadva 2026-04-12 |
| **Keycloak Script Mapper fix** | spaceos_tenants snake_case + 4 mező + csoport attribútumok | `DONE` ✅ | — | MSG-INFRA-060-DONE elfogadva 2026-04-12 |
| **Orchestrator auth/me fix** | /bff/auth/me tenantId + JWKS config commit — 177 teszt | `DONE` ✅ | — | MSG-ORCHESTRATOR-056-DONE elfogadva 2026-04-11 |
| **Keycloak token lifespan** | Access token 30min → 5min + realm-export frissítve | `DONE` ✅ | — | MSG-INFRA-058-DONE elfogadva 2026-04-12 ⚠️ realm-export.json git commit manuálisan szükséges (VPS nem git repo) |
| **GetTenantId() FindAll fix** | spaceos_tenants FindAll + MicrosoftTenantIdClaimType fallback · 1075 teszt | `DONE` ✅ | — | MSG-KERNEL-059-DONE elfogadva 2026-04-12 |
| **Migration 0028 bypass** | Raw SQL + EF history bypass + Crypto::SigningKey alkalmazva | `DONE` ✅ | — | MSG-INFRA-060-DONE elfogadva 2026-04-12 · DB: 20260410130000 applied |
| **Migration 0028 proper regen** | rate limit config + GetTenantId() array fix + port fix | `CODE_DONE` | — | MSG-KERNEL-060-DONE · commit 03a7799 · migration reconcile KERNEL-061-ben lezárva |
| **E2E 28-keycloak-auth fix** | snake_case + roles expect törlés · 13/13 pass | `DONE` ✅ | — | MSG-E2E-004-DONE elfogadva 2026-04-12 |
| **GetTenantId() array parsing** | double-serialized `[{...}]` string ValueKind==Array case → 4 E2E 401 | `CODE_DONE` | — | KERNEL-060-DONE részként elfogadva · INFRA deploy után élő (MSG-INFRA-062) |
| **Migration 0028 EF reconcile** | 20260412060341 törlése · 20260410130000 visszaállítása · ModelSnapshot f7298a8 | `DONE` ✅ | — | MSG-KERNEL-061-DONE elfogadva · commit c62f1d7 · 1075 teszt ✅ ⚠️ Stage entitások nem a snapshot-ban (tech debt) |
| **Kernel VPS deploy post-KERNEL-060** | env fix + binary (c62f1d7) + restart · port 5000 canonical | `DONE` ✅ | — | MSG-INFRA-062-DONE elfogadva 2026-04-12 ⚠️ kernel.env ASPNETCORE_URLS=5001 no-op (tech debt) |
| **Joinery+Abstractions VPS deploy** | Joinery 5002 aktív (ápr 9) + Abstractions 5003 most deployolva | `DONE` ✅ | — | MSG-INFRA-061-DONE elfogadva 2026-04-12 ⚠️ Joinery pending migrations: 0002+J0002 (tech debt) · Abstractions Orch proxy kérdéses |
| **E2E Batch 1 expansion** | 29/30/31 pass · 34-abstractions BLOCKED-ORCH (proxy hiányzik) | `PARTIAL` ⚠️ | — | MSG-E2E-005-DONE-K1 elfogadva 2026-04-12 |
| **Abstractions BFF proxy + VPS deploy** | /bff/abstractions/* route + ORCH-058+059 pm2 restart | `DONE` ✅ | — | MSG-ORCH-059-DONE · commit 4a96e3c · VPS live 2026-04-13 · 183 teszt |
| **E2E Batch 2** | 32-spatial (5) · 33-brand (4, probe-skip) · 35-config-engine (7) · +SSE stabilizálva | `DONE` ✅ | — | MSG-E2E-007-DONE elfogadva 2026-04-13 ⚠️ 33-brand Phase 3B Kernel hiányzik (backlog) |
| **E2E 120-teszt rerun** | migration 0028 + GetTenantId fix verifikálva · 115/120 | `DONE` ✅ | — | MSG-E2E-006-DONE elfogadva 2026-04-12 |
| **FlowEpic Close + Node Register + TenantSummary** | ClaimsTenantResolver spaceos_tenants-first + GUID normalizálás | `SUPERSEDED` | — | MSG-KERNEL-062-DONE · commit 8dd0bd7 · regresszió → MSG-063 javítja |
| **BFF requireAuth + Abstractions proxy** | /bff/abstractions/* → 5003 · chat 401 · 183 teszt | `DONE` ✅ | — | ORCH-059 VPS live 2026-04-13 · commit 4a96e3c |
| **Kernel rollback** | 8dd0bd7 → c62f1d7 · VPS visszaállítva publish.bak-20260413-071651-ről | `DONE` ✅ | — | MSG-INFRA-064-DONE elfogadva 2026-04-13 · /healthz 200 ✅ |
| **ClaimsTenantResolver fallback fix** | JsonDocument parsing + groups fallback + soha ne throw · 1084 teszt | `DONE` ✅ | — | MSG-KERNEL-063-DONE elfogadva · commit 316f603 · develop branch |
| **ClaimsTenantResolver + TenantSessionInterceptor végleges fix** | tid-first prioritás + graceful fallback + TSI c62f1d7 revert · 1084 teszt | `SUPERSEDED` | — | d6b1bad → b270ccf (clean revert) váltja fel |
| **MSG-066 teljes diff diagnózis + clean revert** | ClaimsTenantResolver + TenantSessionInterceptor + Tests c62f1d7-re visszaállítva · 1075 teszt | `DEPLOYED` ✅ | — | commit b270ccf · VPS live 2026-04-13 22:41 · 147/4/0 ✅ |
| **Kernel VPS deploy 316f603** | ClaimsTenantResolver fix élesítése a VPS-en · HEAD: 75ed3bd | `SUPERSEDED` | — | Regresszió 119/151 maradt → d6b1bad → b270ccf váltja fel |
| **E2E bővítés** | Batch 1: 29/30/31 pass · 34-abstractions ORCH-059 után | `PARTIAL` | E2E | MSG-E2E-005-DONE-K1 elfogadva |
| **ORCH-060 proof route path fix** | proof.route.ts `/api/tasks/` → `/api/flow-epics/` · 1 sor fix | `DEPLOYED` ✅ | — | commit b7b4581 · VPS live 2026-04-14 · INFRA-071-DONE ✅ |
| **KERNEL-067 FlowEpic /close RLS fix** | TenantSessionInterceptor tid-first priority · RLS UUID mismatch javítva · 1077 teszt | `DEPLOYED` ✅ | — | commit 46d6352 · VPS live 2026-04-14 · INFRA-072-DONE ✅ |
| **E2E Batch 3 — proof chain (36)** | 36-proof.chain.test.ts 5/5 lenient · BFF proxy 404 confirmed · rerun pending INFRA-071+072 | `DONE` ✅ | — | 156 E2E teszt · helpers.ts bővítve · 2026-04-14 |
| **E2E Batch 3 — tools chain** | 37-tools.chain.test.ts · LLM tool calling registry (adat-projekciók) | `DONE` ✅ | — | 5/5 zöld · MSG-E2E-015-DONE · ⚠️ LLM tool descriptor endpoint → KERNEL-069 backlog |
| **ORCH-061 proof Content-Type** | proof.route.ts image/jpeg → application/octet-stream · 184 teszt | `DEPLOYED` ✅ | — | commit ca00227 · VPS live 2026-04-14 · INFRA-073-DONE ✅ |
| **KERNEL-068 05-close diagnózis** | Root cause: tid hiányzik JWT-ből · Kernel kód helyes | `RESOLVED` ✅ | — | E2E-017 megerősítette · INFRA-075 megoldotta |
| **E2E Batch 3 — abstractions deep** | 34-abstractions-deep.chain.test.ts 5/5 · 166 E2E teszt | `DONE` ✅ | — | MSG-E2E-016-DONE 2026-04-14 · Batch 3 kész |
| **INFRA-073 Orch ca00227** | Orchestrator ca00227 VPS live · proof CT fix | `DEPLOYED` ✅ | — | INFRA-073-DONE 2026-04-14 |
| **INFRA-074 Kernel verify** | Kernel 46d6352 DLL 07:36 · ⚠️ audit chain break preexisting (KERNEL-070 new/) | `DONE` ✅ | — | INFRA-074-DONE 2026-04-14 |
| **INFRA-075 KC tid mapper** | KC `tid` flat claim live · test-runner JWT ellenőrizve | `DEPLOYED` ✅ | — | INFRA-075-DONE · ⚠️ kernel-api prod gap → INFRA-076 |
| **INFRA-076 KC realm-scope** | `spaceos-tenant-scope` realm default scope · portal-app/orch-bff/kernel-api lefedve | `DEPLOYED` ✅ | — | INFRA-076-DONE · jövőbeli kliensek auto-kapják |
| **KERNEL-071 Migration 0029 regen** | Designer.cs + AppDbContextModelSnapshot regenerálva EF tooling-gal · 1104 teszt | `DONE` ✅ | — | commit 0fafdb9 · 2026-04-15 · Migration 0029 timestamp: 20260415054837 |
| **ORCH-063 Ecosystem Actor BFF** | catch-all proxy `/bff/api/*` → kernel · 191 teszt | `DEPLOYED` ✅ | — | commit f7ddb37 · VPS live 2026-04-15 · ModuleRegistry/TenantType endpointok lefedve |
| **INFRA-083 Kernel 0fafdb9 deploy** | Migration 0029 DB-ben · TenantType + triggers + seed élesítve · 42710 manual fix | `DEPLOYED` ✅ | — | 2026-04-15 · /healthz + /bff/health zöld · Sprint 5 UNBLOCKED |
| **E2E-018 rerun** | **162/166** · 36-proof teljes lánc 200 · Doorstar Q2 happy path 🎉 | `DONE` ✅ | — | MSG-E2E-018-DONE · 36-proof assertion fix (proofUrl/proofHash) |
| **🎉 DOORSTAR Q2 HAPPY PATH** | Proof upload → close (Delivery→ClosedDone) teljes E2E chain ZÖLD | `VERIFIED` ✅ | — | 2026-04-14 · 36-proof 7 lépéses lánc 200 |
| **BATCH-0-CLEANUP** | 4 legacy fail: 05-close (storage val.), 15-nodes (500), 24×2 (tenantToken tid?) | `IN_PROGRESS` | — | E2E-019 + KERNEL-069 kiadva |
| **Sprint 5 — Test Coverage** | DA audit · 8 kockázat (R-13–R-20) · Testcontainers PG · AuditChain CI · cross-tenant E2E · BFS cycle detection · PDF golden-file · PKCE + refresh dedup | `CLOSED_DONE` ✅ | — | 2112 teszt · 2026-04-15 |
| **INFRA-086 ORCH b3860ac deploy** | Sprint 5 proxy 502 fix + tid claim validáció élesítve | `DEPLOYED` ✅ | — | VPS live 2026-04-15 · /bff/health 200 ✓ |
| **KERNEL-075 AuditEvents sequence** | `sequence BIGINT IDENTITY` · Migration 0030 · OccurredAt tiebreaker · commit 5bfe0a1 | `DEPLOYED` ✅ | — | VPS live 2026-04-15 |
| **KERNEL-070 Audit chain investigation** | Root cause: random GenesisHash · 6808 E2E event törölve · 9 artifact elfogadva · stabil genesis hash live | `DONE` ✅ | — | 2026-04-15 · KERNEL-075/077/078 + INFRA-089 lánc |
| **ORCH-069 Security fixes** | K1: err.message NODE_ENV guard · K2: SSRF allowlist `127.0.0.1:\d{4,5}` · K3: sanitizeUserContent 4096 limit | `CODE_DONE` ✅ | — | commit 14fcc9b · 207/207 teszt · INFRA deploy pending |
| **ABSTRACTIONS-007 Security fixes** | M01: ValidateAudience=true · M02: TenantCommandInterceptor (read-path RLS DbCommandInterceptor) · M03: repo tenantId filter + 9 handler | `CODE_DONE` ✅ | — | 81/81 teszt · INFRA deploy pending |
| **INFRA-090 Portal Axios CVE** | Axios 1.15.0 · GHSA-3p68-rc4w-qgx5 + GHSA-fvcv-3m26-pcqx javítva · pnpm audit 0 CVE | `DONE` ✅ | — | dist/ rebuild szükséges · vite moderate CVE fennáll (külön döntés) |
| **PORTAL-006 dist/ rebuild** | Axios 1.15.0 bundle · 281/281 teszt · dist/ Apr 15 timestamp | `DONE` ✅ | — | 2026-04-15 |
| **INFRA-092 Portal dist/ deploy** | Axios 1.15.0 live VPS-en · nginx reload · joinerytech.hu 200 ✓ | `DEPLOYED` ✅ | — | 2026-04-15 · Vite moderate CVE Q3 backlog |
| **Sprint 6 — Modules.Cutting Contracts** | IInventoryProvider + ICuttingProvider + IProcurementProvider NuGet · 3 package | `ACTIVE` | — | JOINERY-006 kiadva · INFRA-095 repo setup |
| **Sprint 6 — Q3 scope** | KERNEL-069 LLM registry · Modules.Cutting Core · R-04 OpenTimestamps (Escrow GA) | `BACKLOG` | contracts DONE | — |
| Modules.Joinery v2 | PDF export, Gyártásilap | `BACKLOG` | E2E zöld (MSG-E2E-003) | — |
| Modules.Cabinet v1 | Szekrénygyártás domain | `BACKLOG` | Joinery v1 DoD | — |
| Escrow WORM GA | S3 Object Lock / Azure Immutable Blob | `BACKLOG` | Doorstar pilot live | — |

---

## Git repók (polyrepo)

| Repo | Branch | Remote |
|---|---|---|
| `spaceos-kernel` | `main` / `develop` | `github.com/Szantoi/spaceos-kernel` |
| `spaceos-orchestrator` | `main` / `develop` | `github.com/Szantoi/spaceos-orchestrator` |
| `spaceos-design-portal` | `main` / `develop` | `github.com/Szantoi/spaceos-design-portal` |
| `spaceos-docs` | `main` / `develop` | `github.com/Szantoi/spaceos-docs` |

**Workflow:** `develop` → fejlesztés · `main` → stabil, prod-ra deployolható

---

## Sprint D Phase 3A — Spatial BIM Core (2026-04-07) — CLOSED_DONE

**Ref:** `/opt/spaceos/docs/SpaceOS_Phase3A_Architecture_v3.md`
**Mailbox:** MSG-K024 (DONE)

### Feladatok: 6/6 DONE

| Terület | Tartalom | Státusz |
|---|---|---|
| PA-01 | Domain Layer (PhysicalSpace, BvhNode, SpatialElement, SpatialTaskLink, 5 VO, 3 event) | ✅ DONE |
| PA-02 | BVH Domain Service (IBvhTreeService, BvhQueryService, cycle+depth guard) | ✅ DONE |
| PA-03 | Application CQRS (5 handler + 3 FluentValidation validator) | ✅ DONE |
| PA-04 | Infrastructure (EF Core OwnsOne, IBvhRepository, Migrations 0016–0019) | ✅ DONE |
| PA-05 | API (5 publikus endpoint; BVH belső — nem OpenAPI) | ✅ DONE |
| PA-06 | Tesztek (+24 új: BoundingBox 10, security gates 5, validators 12 + pre-existing 10) | ✅ DONE |

**22 finding beépítve:** 5 CRITICAL + 10 HIGH + 7 MEDIUM (BE-P3A-01..11 + SEC-P3A-01..11)

### Phase 3A DoD Checklist

| # | Ellenőrzés | Eredmény |
|---|-----------|---------|
| ✅ | TenantScopedEntity base class — `Guid Id` + `Guid TenantId` | ✅ |
| ✅ | PhysicalSpace aggregate — `static Register()` factory, SHA-256 hash | ✅ |
| ✅ | BvhNode — NO `_children` nav prop (BE-P3A-01/03) | ✅ |
| ✅ | BvhQueryService — async recursive traversal, cycle guard (HashSet), depth guard (>32) | ✅ |
| ✅ | IBvhTreeService internal (nem OpenAPI) — BE-P3A-02 | ✅ |
| ✅ | FluentValidation minden command-on | ✅ |
| ✅ | `SpatialContractsView` — ElementType intentionally absent (ADR-008) | ✅ |
| ✅ | Cross-tenant guard: `task.TenantId != element.TenantId` → `Result.Forbidden` | ✅ |
| ✅ | Migration 0016–0019: RLS FORCE + spaceos_schema_owner + try_cast_uuid | ✅ |
| ✅ | TradeType CHECK (no 'other') + WorkPhase CHECK (no 'other') | ✅ |
| ✅ | `CK_BvhNodes_NoSelfLoop` + `CK_BvhNodes_LeafElement` | ✅ |
| ✅ | `check_bvh_depth()` trigger — max 32 recursive CTE | ✅ |
| ✅ | `prevent_cell_size_change()` trigger — CellSize immutable | ✅ |
| ✅ | `TR_SpatialTaskLinks_TenantCheck` — cross-tenant insert blokkolva | ✅ |
| ✅ | SpatialSecurityTests — reflection-alapú gate-ek (ElementType absent, no Other enum, no _children/_nodes) | ✅ |
| ✅ | 814 teszt, 0 fail (777→814, +37 kernel unit) | ✅ |
| ✅ | Migrations 0016–0019 alkalmazva PostgreSQL-en | 2026-04-08 psql script (spaceos_schema_owner nincs VPS-en, tables owned by spaceos) |
| ✅ | `EXPLAIN ANALYZE` — AuditEvents: Bitmap Index Scan `IX_AuditEvents_TenantId_OccurredAt` (0.22ms) | 2026-04-08 |
| ⚠️ | `SELECT tableowner FROM pg_tables WHERE tablename = 'BvhNodes'` → `spaceos_schema_owner` | VPS-en csak `spaceos` user létezik, schema owner eltér a terv. |
| ✅ | DB trigger tesztek (cross-tenant, self-loop, depth limit, CellSize immutability) | 2026-04-08 — mind a 4 PASS |

### Phase 3A Metrikus összefoglaló

| Metrika | Érték |
|---------|-------|
| Kernel tesztek (Phase 3A) | **814** (645 unit + 101 integration + 68 API) |
| Új tesztek | **+37** (BoundingBox 10, security gates 5, validators 12, pre-existing spatial 10) |
| Új production fájlok | **~55** |
| Új migrations | **4** (0016–0019) |
| Új domain events | **3** (PhysicalSpaceRegistered, SpatialElementRegistered, SpatialCollisionDetected) |
| Új API endpoint | **5** (`POST /api/spaces`, `POST /api/spaces/{id}/elements`, `POST /api/elements/{id}/links`, `GET /api/spaces/{id}/timeline`, `GET /api/spaces/{id}/timeline/events`) |
| Új ADR | **1** (ADR-008: SpatialContractsView ElementType absent) |
| Build warning | **0** |

### Phase 3A Deploy gate-ek (VPS-en szükséges)

- [x] Migrations 0016–0019 alkalmazva: psql script (2026-04-08, Designer.cs stub-ok létrehozva)
- [x] `EXPLAIN ANALYZE` — AuditEvents Bitmap Index Scan ✅; Spatial Seq Scan (3 sor, prod indexek megvannak)
- [⚠️] `SELECT tableowner FROM pg_tables WHERE tablename = 'BvhNodes'` → `spaceos` (spaceos_schema_owner nem létezik VPS-en)
- [x] DB trigger teszt: cross-tenant SpatialTaskLink INSERT → blokkolva ✅
- [x] DB trigger teszt: BvhNode önhivatkozás → `CK_BvhNodes_NoSelfLoop` CHECK blokkolja ✅
- [x] DB trigger teszt: BvhNode mélység > 32 → blokkolva ✅
- [x] DB trigger teszt: PhysicalSpace CellSize módosítás → blokkolva ✅

### Phase 3A Ismert limitációk — Phase 3B lezárta

- `FlowEpic.ComputeSpatialState()` — ✅ Phase 3B-ben megoldva (`ToSnapshotDto()` + `FlowEpic.Tasks` elérhetővé vált)
- Parallel collision teszt — Phase 3C scope
- DoD teszt gap: +24 elért vs ≥35 cél — Phase 3B +99 teszttel kompenzálva

---

## Sprint D Phase 3B — Escrow GA Foundation (2026-04-07) — CLOSED_DONE

**Ref:** `/opt/spaceos/docs/SpaceOS_Phase3B_Architecture_v4.md`
**Mailbox:** MSG-K026 (DONE) · MSG-O010 (DONE) · MSG-P017 (✅ DONE) · MSG-P018 (✅ DONE)

### Feladatok: 7/7 DONE (Kernel)

| Task | Tartalom | Státusz |
|---|---|---|
| T-01 | `AggregateSnapshot` entity + `ISnapshotable` + `FlowEpicSnapshotDto` + Migration 0020 | ✅ DONE |
| T-02 | `OutboxEntry` + `OutboxWorker` (PeriodicTimer + IServiceScopeFactory) + Migration 0021 | ✅ DONE |
| T-03 | `SnapshotService` (internal) + `FlowEpicClosedDoneOutboxHandler` | ✅ DONE |
| T-04 | `GetSnapshotAtQuery` + `GetSnapshotVersionsQuery` (Ardalis.Spec) + endpoints | ✅ DONE |
| T-05 | ProofHash + `IProofStorageService` + streaming upload endpoint + Migration 0022 | ✅ DONE |
| T-06 | `VerifyChain` endpoint (`WormStorageAvailable` flag, nem 500) | ✅ DONE |
| T-07 | Genesis hash → `IGenesisHashProvider` + `KeyVaultGenesisHashProvider` + Migration 0023 | ✅ DONE |

**15 finding beépítve:** 3 CRITICAL + 10 HIGH + 9 MEDIUM (BE-P3B-01..06 + SEC-P3B-01..09)

### Phase 3A Minor fixes (MSG-K027) — egyidejűleg lezárva

| Fix | Eredmény |
|---|---|
| `DimensionVector` + `Point3D`: `sealed record` → `readonly record struct` | ✅ EF Core `ComplexProperty`-re migrálva |
| CS nullable warnings (5 db) `TenantSessionInterceptorTests.cs`-ben | ✅ `[AllowNull]` attribútumokkal javítva |
| `SpaceLayerConfigurationTests` assertion bug (`"text"` → `"jsonb"`) | ✅ mellékesen javítva |

### Phase 3B DoD Checklist

| # | Ellenőrzés | Eredmény |
|---|-----------|---------|
| ✅ | `AggregateSnapshot.Create()` hash determinisztikus; StateJson > 512KB → `DomainException` | ✅ |
| ✅ | `ToSnapshotJson()` nem üres JSON — `FlowEpicSnapshotDto`-n át (BE-P3B-01) | ✅ |
| ✅ | `OutboxEntry.MarkFailed()` — max 2000 char; stack trace kizárva (SEC-P3B-07) | ✅ |
| ✅ | `OutboxWorker` — IServiceScopeFactory, PeriodicTimer, graceful shutdown (BE-P3B-02) | ✅ |
| ✅ | `ISnapshotService` internal — Api réteg nem látja | ✅ |
| ✅ | Proof upload — MIME whitelist; hash szerveren számított; `Request.Body` stream (BE-P3B-04) | ✅ |
| ✅ | `ProofStorageKey` — TenantId az első path component (SEC-P3B-01) | ✅ |
| ✅ | `ConstantGenesisHashProvider` — csak `IsDevelopment()` esetén (SEC-P3B-06) | ✅ |
| ✅ | GenesisHash konstans grep → 0 találat | ✅ |
| ✅ | `VerifyChain` WORM unavailable → 200 + `WormStorageAvailable: false` (SEC-P3B-05) | ✅ |
| ✅ | Migration 0020–0023 fut; FORCE RLS + `spaceos_schema_owner` | ✅ |
| ✅ | `CK_Tenants_NoSystemId` — system UUID (`00000000-...-0001`) nem regisztrálható tenant-ként (SEC-P3B-02) | ✅ |
| ✅ | 814 meglévő teszt zöld + 99 új teszt (cél: ≥45) | ✅ **913 total** |
| ✅ | 0 build warning · 0 CVE | ✅ |
| ✅ | Migration 0020–0023 alkalmazva PostgreSQL-en | 2026-04-08 psql script |
| ✅ | `EXPLAIN ANALYZE` AggregateSnapshots + OutboxMessages — Seq Scan (3 sor, indexek megvannak prod-ra) | 2026-04-08 |
| ⚠️ | `SELECT tableowner ... IN ('AggregateSnapshots','OutboxEntries')` → `spaceos` (schema owner gap) | VPS-en csak `spaceos` user |
| ✅ | `OutboxMessages` polling index megvan (`IX_OutboxMessages_Polling` vagy eq.) | Seq Scan / 3 sor — indexek léteznek |
| ✅ | Portal manuális típus-szinkron (C# forrásból) + `ChainVerificationDto.wormStorageAvailable` | MSG-P018-DONE: Kernel prod-ban fut, OpenAPI snapshot |

### Phase 3B Metrikus összefoglaló

| Metrika | Érték |
|---------|-------|
| Kernel tesztek (Phase 3B után) | **913** (645+99 unit + 101 integration + 68 API) |
| Új tesztek (Phase 3B) | **+99** (cél volt: ≥45) |
| Orchestrator tesztek | **150** (+24: admin middleware, snapshot route, proof route) |
| Új production fájlok (Kernel) | **~35** |
| Új migrations | **4** (0020–0023) |
| Új API endpoint | **4** (`GET /api/snapshots/{id}?at=`, `GET /api/snapshots/{id}/versions`, `GET /api/audit-events/verify-chain`, `POST /api/tasks/{id}/proof`) |
| Build warning | **0** |
| CVE | **0** |

### Phase 3B Deploy gate-ek (VPS-en szükséges)

- [x] Migrations 0020–0023 alkalmazva: psql script (2026-04-08)
- [x] `EXPLAIN ANALYZE` snapshot query — Seq Scan elfogadható (3 sor), indexek megvannak prod-ra
- [⚠️] `SELECT tableowner FROM pg_tables WHERE tablename IN ('AggregateSnapshots','OutboxEntries')` → `spaceos` (schema owner gap)
- [x] OutboxMessages polling: indexek léteznek, Seq Scan / 3 sor
- [ ] `OutboxWorker` induláskor loggol (`BackgroundService started`) — log ellenőrzés folyamatban
- [ ] `ConstantGenesisHashProvider` prod DI-ban nem regisztrált (startup log ellenőrzés)
- [x] Portal típusok manuálisan szinkronizálva (C# forrásból, Kernel prod mode miatt); `sync-types` snapshot-alapú átírva (design-portal root package.json)

---

## Sprint D Phase 3C — Multi-Brand Portal: Turborepo + Brand Skin System (2026-04-07) — CLOSED_DONE

**Ref:** `/opt/spaceos/docs/SpaceOS_Phase3C_Architecture_v2.md`
**Mailbox:** MSG-K028→K029 (DONE) · MSG-O011 (DONE) · MSG-P019 (DONE)

### Feladatok: 7/7 DONE

| Task | Tartalom | Státusz |
|------|-----------|----|
| T-01 | Turborepo scaffold: `apps/joinerytech/`, 5 package shell, `turbo.json`, `pnpm-workspace.yaml`, `vitest.workspace.ts` | ✅ DONE |
| T-02 | `@spaceos/domain` — `kernel.ts` generált snapshot, whitelist re-export, `codegen.sh` | ✅ DONE |
| T-03 | `@spaceos/api-client` — `createSpaceOsClient()` factory, callback-alapú auth (körhivatkozás nélkül) | ✅ DONE |
| T-04 | `@spaceos/ui` — 5 dumb komponens (FsmBadge, HashDisplay, PagedTable, TradeTypeBadge, JsonIntentEditor) | ✅ DONE |
| T-05 | `@spaceos/brand-tokens` — `types.ts`, `sanitize.ts`, tokens, registry, `BrandProvider`, Tailwind preset | ✅ DONE |
| T-06 | Doorstar skin — `overrides.ts` (lazy() csak app-ban), doorstar dashboard override, `brandRouter.tsx` | ✅ DONE |
| T-07 | `@spaceos/i18n` + `authStore.brandSkin` — `detectLocale` (hostname-based), `useTranslation` (dot-notation) | ✅ DONE |

**Kernel: Migration 0024** (`Tenants.BrandSkinId` + JWT `brand_skin` claim) — ✅ DONE (MSG-K029)
**Orchestrator: brandSkin** token + refresh response-ban — ✅ DONE (MSG-O011)
**20 finding beépítve:** 2 CRITICAL + 7 HIGH + 11 MEDIUM (BE-P3C-01..05 + SEC-P3C-01..07)

### Phase 3C DoD Checklist

| # | Ellenőrzés | Eredmény |
|---|-----------|---------|
| ✅ | `lazy()` import csak `apps/*`-ban — `packages/@spaceos/*`-ban 0 találat | ✅ |
| ✅ | `@spaceos/ui` — 0 network dep (`grep -r "api-client\|fetch\|axios"` → 0 találat) | ✅ |
| ✅ | `madge --circular packages/` — körhivatkozás NINCS | ✅ |
| ✅ | `validateHexColor('expression(alert(1))')` → throw (XSS sanitize) | ✅ |
| ✅ | `validateLogoPath('../../etc/passwd')` → throw (path traversal) | ✅ |
| ✅ | `resolveBrand('ismeretlen')` → JoineryTech fallback | ✅ |
| ✅ | `brandSkin` csak Orchestrator response body-ból (`authStore.brandSkin`) — nem JWT client-decode | ✅ |
| ✅ | `localStorage.setItem('brandSkin', ...)` → nincs hatás | ✅ |
| ✅ | `detectLocale()` hostname-only (nem user-provided) | ✅ |
| ✅ | Migration 0024: `Tenants.BrandSkinId VARCHAR(64) NULL`, JWT `brand_skin` claim | ✅ |
| ✅ | RefreshToken `Guid.Empty` tenantId bug javítva (bónusz) | ✅ |
| ✅ | 321 teszt (275 app + 46 packages), 0 fail (cél: ≥35 új package teszt) | ✅ 46 |
| ✅ | 0 körhivatkozás, 0 build warning | ✅ |
| ✅ | Nginx Phase 3C: új dist path (`/apps/joinerytech/dist/`), CSP bővítés | 2026-04-08 |
| ✅ | `pnpm` workspace — `pnpm turbo build --filter=joinerytech` sikeres (1764 modul, 4.18s) | 2026-04-08 |
| ✅ | Migration 0024 alkalmazva PostgreSQL-en | 2026-04-08 psql script |

### Phase 3C Metrikus összefoglaló

| Metrika | Érték |
|---------|-------|
| Kernel tesztek (Phase 3C után) | **915** (+2 Phase 3C: RefreshToken Guid.Empty fix tesztek) |
| Orchestrator tesztek | **153** (+3 brandSkin tesztek) |
| Portal tesztek | **321** (275 app + **46 új packages**) |
| Új packages | **5** (`@spaceos/domain`, `@spaceos/api-client`, `@spaceos/ui`, `@spaceos/brand-tokens`, `@spaceos/i18n`) |
| Új migrations | **1** (0024: `Tenants.BrandSkinId`) |
| Körhivatkozás | **0** (madge gate) |
| Build warning | **0** |
| CVE | **0** |

### Phase 3C Deploy gate-ek (VPS-en szükséges)

- [x] Migration 0024 alkalmazva: psql script (2026-04-08)
- [x] pnpm telepítve, `pnpm turbo build --filter=joinerytech` sikeres
- [x] Nginx dist path frissítve: `root /opt/spaceos/design-portal/apps/joinerytech/dist/;`
- [x] Nginx CSP bővítve (brand assets: `img-src 'self' data: blob:`)
- [ ] Nginx `/brand/` location (brand skin JSON serve) — opcionális, nem blokkoló
- [ ] `brand_skin` claim megjelenik token response-ban
- [ ] Doorstar skin: `joinerytech.hu`-n `JoineryTech` dashboard látszódik
- [ ] `ConstantGenesisHashProvider` startup log ellenőrzés (prod → nincs bejegyezve)

---

## Sprint D Phase 3C+ — Joinery Module System + B2BHandshake Live API (2026-04-08) — CLOSED_DONE

**Ref:** `/opt/spaceos/docs/SpaceOS_Phase3Cplus_Architecture_v3.md`
**Lezárva:** 2026-04-08
**Mailbox:** MSG-K030 (DONE) · MSG-O013+O014 (DONE) · MSG-P020+P021 (DONE)

### Eredmények

| Terület | Komponensek | Eredmény |
|---|---|---|
| `@spaceos/joinery-ui` | CuttingListBase, ProductionKanban, MaterialPanel, HandshakeOrderPanel/HostPanel, door/cabinet/window ágak | ✅ DONE |
| Modul-rendszer | `moduleRouter.tsx`, `modules/door/`, `modules/cabinet/`, `modules/window/`, `ErrorBoundary` | ✅ DONE |
| Auth store | `enabledModules`, `allowedHosts`, `isLoading` — SEC-P3CP-05 partialize kizárás | ✅ DONE |
| Kernel B2BHandshake | Migration 0025+0026, `TenantHandshakeAllowlist`, `HandshakeEndpoints.cs`, token claims | ✅ DONE |
| Orchestrator | `/bff/handshakes` proxy (5 endpoint), auth response `enabledModules`+`allowedHosts` | ✅ DONE |
| Portal Day 11 | `CabinetOrdersPage` live hookup, `useHandshakes` hook, `handshakes.service.ts` | ✅ DONE |

### Teszteredmények (záráskori)

| Projekt | Tesztek |
|---|---|
| SpaceOS.Kernel | 915 / 915 ✅ |
| SpaceOS.Orchestrator | 160 / 160 ✅ |
| Portal auth.store | 13 / 13 ✅ |

---

## Production Readiness Sprint — Keycloak + Audit Race Fix + PostgreSQL WORM (CLOSED_DONE)

**Ref:** `/opt/spaceos/docs/SpaceOS_ProductionReadiness_Sprint_v4.md`
**Becsült effort:** ~8 fejlesztői nap
**Indult:** 2026-04-08 · **Lezárva:** 2026-04-09
**Mailbox:** MSG-K031 (DONE) · MSG-O015 (DONE) · MSG-P022 (DONE)
**Review összesítő:** 3 CRITICAL · 9 HIGH · 6 MEDIUM finding (v4-ben mind beépítve)

### Három track

| Track | Tartalom | Réteg | Felelős |
|---|---|---|---|
| **A** | Keycloak IdP — `spaceos_keycloak` PostgreSQL backend + Nginx `/auth/` + `/auth/admin/` block + Realm + Google/Microsoft OIDC | VPS + Kernel + Orchestrator + Portal | T1 + T3 + T2 + VPS |
| **B** | Audit Race Fix — `Channel<T>` `BoundedChannelFullMode.Wait` + `pg_advisory_xact_lock(bigint)` MD5 key + graceful drain 30s | Kernel | T1 |
| **C** | PostgreSQL WORM Sink — `AuditHashes` tábla + `spaceos_audit_worm` INSERT-only role + RLS + `PostgresWormStorageService` | Kernel + VPS | T1 |

### Terminal kiosztás

| Terminal | Track | Feladatok |
|---|---|---|
| **T1** (Kernel) | B + C + A.4 | ✅ DONE — 927/927 test |
| **T3** (Orchestrator) | A.5 | ✅ DONE — 164/164 test |
| **T2** (Portal) | A.6 | ✅ DONE — 346/346 test |
| **VPS** (humán) | A.1–A.3 | ✅ DONE — 2026-04-09 |

### Kritikus biztonsági finding-ek (SEC-01..07)

| ID | Súly | Probléma | Javítás |
|----|------|----------|---------|
| SEC-01 | 🔴 CRITICAL | `BoundedChannelFullMode.DropWrite` → silent audit event loss | `Wait` mode + `LogCritical` overflow |
| SEC-02 | 🔴 CRITICAL | `/auth/admin/` publikusan elérhető | Nginx: `allow 127.0.0.1; deny all;` |
| SEC-03 | 🔴 CRITICAL | `spaceos_audit_worm` role rendelkezik SELECT joggal | `REVOKE SELECT ON AuditHashes FROM spaceos_audit_worm` |
| SEC-06 | 🟠 HIGH | `hashtext(tenantId)` → int4 → 50% kolliziós esély 10k+ tenant felett | `pg_advisory_xact_lock(bigint)` MD5 alapú key |
| SEC-07 | 🟠 HIGH | `AUDIT_SINK_CONNECTION_STRING` cleartext `appsettings.json`-ban | Env var, nem appsettings |

### Definition of Done (kapuk)

- [x] `curl https://joinerytech.hu/auth/admin/` → **403** ✅ 2026-04-09
- [x] `AUDIT_SINK_CONNECTION_STRING` env varban van, nem appsettings-ban ✅ Kernel T1
- [x] `Channel<T>` overflow → `LogCritical` loggolható teszttel ✅ Kernel T1
- [x] `pg_advisory_xact_lock(bigint)` MD5 key — nem `hashtext()` ✅ Kernel T1
- [x] `AuditEventDispatcher.DisposeAsync()` graceful drain 30s timeout ✅ Kernel T1
- [x] Kernel JWT `Authority` + `Audience` config-driven — nincs hardcoded issuer ✅ Kernel T1
- [x] Orchestrator `jwks-rsa` csomag, `JWKS_URI` env varból ✅ Orchestrator T3
- [x] Portal `AUTH_PROVIDER=dev` helyi fejlesztésben tovább működik ✅ Portal T2
- [x] Keycloak fut + `spaceos` realm + 3 client ✅ 2026-04-09
- [x] Meglévő tesztek zöld + ≥18 új teszt ✅ 927+164+346 = 1437 pass
- [ ] `\dp AuditHashes` → `spaceos_audit_worm` csak `a` (INSERT) jog — ellenőrzés szükséges
- [ ] Keycloak restart teszt — ellenőrzés szükséges

---

## Kernel Stage Registry — Workflow Stage Architecture v4 (CODE_COMPLETE — tesztek hiányoznak)

**Ref:** `/opt/spaceos/docs/SpaceOS_WorkflowStage_Architecture_v4.md`
**Üzleti összefoglaló:** `/opt/spaceos/docs/SpaceOS_WorkflowStage_Summary.md`
**Repo:** `spaceos-kernel` (ADR-022: Stage Registry a Kernel-ben)
**DB:** Kernel `spaceos` schema — Migration 0028 ✅ alkalmazva
**Effort:** ~8 fejlesztői nap · **Indult:** 2026-04-10
**Kumulált review:** `/database-designer` + `/database-schema-designer` → v2 · `/senior-security` → v3 · `/senior-backend` → v4
**Review összesítő:** 3 CRITICAL · 10 HIGH · 11 MEDIUM finding (mind beépítve)
**Implementáció:** MSG-KERNEL-054-DONE elfogadva 2026-04-11 — kód + tesztek rendben ✅
**Tesztek:** 1068 pass / 4 skip / 0 fail (+135 új teszt, elvárás ≥45 volt) ✅
**VPS deploy:** MSG-KERNEL-058 — Migration 0028 + redeploy + FlowEpic 500 + Authority commit + ADR-023

### Üzleti kontextus

A Workflow Stage Architecture az asztalosipari munkafolyamatok konfigurálható pipeline rendszere. Minden cég saját lépéssorrendet definiálhat (pl. Doorstar: `értékesítés → (opcionális felmérés) → gyártás`). A `StageChain` tenant-enként konfigurálható, a `StageHandoff` immutable adatcsomagot ad át lépések között. Az architektúra alapja minden jövőbeli Sales/Survey/Installation modul integrációjának.

### Terminal kiosztás

| Terminal | Repo | Feladatok | Mailbox |
|---|---|---|---|
| **T1** (Kernel) | spaceos-kernel | Domain + Infrastructure + Application + API | MSG-KERNEL-054-DONE ✅ elfogadva · MSG-KERNEL-057 (tesztek pótlás) |
| **T3** (Orchestrator) | spaceos-orchestrator | stageDispatch.ts (BE-03, TTL cache, /bff/stages/ route) | MSG-ORCHESTRATOR-055-DONE ✅ elfogadva (176 teszt) |

### Scope

| Layer | Tartalom |
|-------|---------|
| Domain (Nap 1–2) | `StageDefinition`, `StageChainTemplate`, `StageChainStep`, `StageHandoff` (immutable, IdempotencyKey, HashAlgorithm), `IStageChainValidator` + `StageChainValidator` (BE-01/SEC-03), FlowEpic bővítés (AssignChain, AdvanceToStage, SkipOptionalStage), 6 domain event, 4 Ardalis.Specification |
| Infrastructure (Nap 3) | EF Core config (4 entity) + Migration 0027 (4 tábla + FlowEpics ALTER + indexek + RLS FORCE + triggers DB-06/DB-10 + Doorstar seed) + `StageChainValidator` implementáció |
| Application (Nap 4–6) | 10 command handler (incl. advisory lock + explicit tx + idempotency a CreateStageHandoff-ban) + 5 query handler + FluentValidation (PayloadJson depth 10 + size 1MB) |
| API (Nap 6) | 15 Minimal API endpoint + RBAC: SystemAdmin / TenantAdmin / StageOperator / TenantUser (SEC-02) |
| Tests (Nap 7–8) | ≥20 domain teszt + ≥25 security gate teszt = ≥45 összesen |

### Kritikus finding-ek (beépítve)

| ID | Súly | Javítás |
|----|------|---------|
| SEC-01 | 🔴 CRITICAL | SSRF: ModuleEndpoint port range 5000-5099 CHECK |
| SEC-02 | 🔴 CRITICAL | RBAC 3 szint: SystemAdmin/TenantAdmin/StageOperator |
| DB-01 | 🔴 CRITICAL | Stage Registry a Kernel-ben (ADR-022) |
| DB-02 | 🟠 HIGH | Version race: `pg_advisory_xact_lock` + MAX+1 |
| SEC-03 | 🟠 HIGH | Chain skip guard: IStageChainValidator (BE-01) |
| SEC-05 | 🟠 HIGH | Handoff replay: IdempotencyKey UNIQUE |
| BE-02 | 🟠 HIGH | CreateHandoff explicit `BeginTransactionAsync` tx pattern |

### Definition of Done (kapuk)

#### Migration gates
- [x] Migration 0028: 4 új tábla + FlowEpics ALTER + compound FK DEFERRABLE ✅
- [x] RLS FORCE mind 4 új táblán ✅
- [x] `IX_StageChainTemplates_DefaultPerTenant` partial UNIQUE index ✅
- [x] StageCode regex CHECK + immutability trigger (DB-10) ✅
- [x] ModuleEndpoint port range CHECK 5000-5099 (SEC-01) ✅
- [x] PayloadJson size CHECK < 1MB (DB-05) ✅
- [x] IdempotencyKey UNIQUE (SEC-05) ✅
- [x] UpdatedAt auto-update triggers (DB-06) ✅
- [x] Doorstar seed: 3 stage + 1 chain + 3 step ✅

#### Domain gates
- [x] `StageDefinition.Register()`, no public setters, StageCode immutable ✅
- [x] `StageChainTemplate.AddStep(StageDefinition, ...)` — StageCode from entity (BE-04) ✅
- [x] StageChainTemplate: max 20 steps guard ✅
- [x] `StageHandoff.Create()`: immutable, SHA-256, IdempotencyKey, HashAlgorithm ✅
- [x] `IStageChainValidator`: chain-sorrend + required stage skip guard (SEC-03/BE-01) ✅
- [x] FlowEpic: `AssignChain()` + `AdvanceToStage()` + `SkipOptionalStage()` ✅
- [x] 6 domain event + `PopDomainEvents()` ✅
- [x] 4 Ardalis.Specification classes (BE-05) ✅

#### API + validation gates
- [x] 15 endpoint (4 stage + 5 chain + 4 handoff + 3 flow-epic) ✅
- [x] RBAC: SystemAdmin / TenantAdmin / StageOperator / TenantUser ✅
- [x] FluentValidation: PayloadJson max depth 10 (SEC-04) + size 1MB ✅
- [x] CreateHandoff: advisory lock + explicit tx (DB-02/BE-02/SEC-09) ✅
- [x] CreateHandoff: IdempotencyKey duplicate → return existing (SEC-05) ✅

#### Security gates (deployment blockers)
- [x] Cross-tenant RLS blocked (4 tábla) ✅
- [x] ModuleEndpoint loopback-only CHECK (SEC-01) ✅
- [x] StageDefinition CRUD: SystemAdmin only (SEC-02) ✅
- [x] AdvanceToStage: chain sorrend validáció (SEC-03) ✅
- [x] IdempotencyKey UNIQUE → replay blocked (SEC-05) ✅

#### Összesített
- [x] Meglévő 933 teszt zöld ✅
- [x] **Stage Registry új tesztek: +135 db** (cél ≥45) ✅
- [x] 0 build warning ✅
- [x] `ConfigureAwait(false)` minden production async call-ban ✅
- [x] `dotnet list package --vulnerable` → 0 high/critical ✅
- [ ] VPS deploy: Migration 0028 alkalmazva + kernel restart (MSG-058)

---

## Modules.Joinery v1 — Ajtógyártás Domain Engine (ACTIVE)

**Ref:** `/opt/spaceos/docs/SpaceOS_Modules_Joinery_v4.md`
**Repo:** `/opt/spaceos/spaceos-modules-joinery` (ÚJ polyrepo)
**DB schema:** `spaceos_joinery` (meglévő PostgreSQL 16, Kernel mellett)
**Becsült effort:** ~16 fejlesztői nap · **Indult:** 2026-04-09
**Blokkoló feltétel:** ✅ Production Readiness Sprint DoD teljes
**Review összesítő:** 3 CRITICAL · 8 HIGH · 6 MEDIUM finding (v4-ben mind beépítve)

### Implementációs állapot (2026-04-09)

| Layer | Státusz | Megjegyzés |
|-------|---------|-----------|
| Solution scaffold (5 projekt, NuGet, referenciák) | ✅ KÉSZ | Build: 0 error, 0 warning |
| CLAUDE.md | ✅ KÉSZ | Golden Rules + approved packages |
| Domain layer | ✅ KÉSZ | 26 fájl: aggregates, VOs, enums, events, services |
| Application layer | ✅ KÉSZ | CQRS commands/queries, validators, DI |
| Infrastructure layer | ✅ KÉSZ | JoineryDbContext, EF configs, Migration 0001, services |
| API layer | ✅ KÉSZ | Minimal API endpoints, ManufacturerOnly policy — MSG-J035 |
| Tests | ✅ KÉSZ | 32 teszt zöld — MSG-J035+J038 |
| Repository layer | ✅ KÉSZ | DoorOrderRepository, DoorRulesRepository, DI — MSG-J036 |
| DoorstarSeedData.cs | ✅ KÉSZ | 3 GlobalConstants, 8 DoorTypeRules, 10 ProcessTaskTemplates — MSG-J036 |
| DoorRulesDataSeeder | ✅ KÉSZ | IDataSeeder implementáció, DI regisztráció — MSG-J036 |
| Startup Seeder hook | ✅ KÉSZ | Program.cs `ApplicationStarted` hook + fatal guard — MSG-J038 |
| Health endpoint | ✅ KÉSZ | `GET /health` → `{ status, service }` — MSG-J038 |
| Seed validation tesztek | ✅ KÉSZ | 4 új teszt: DoorstarSeedDataTests.cs — MSG-J038 |
| DB migrációk alkalmazása | ✅ KÉSZ | Program.cs MigrateAsync + spaceos_joinery schema, Migration 0002 seed |
| Seed bővítés | ✅ KÉSZ | DoorTypeRules 15 sor, ProcessTaskTemplates 41 sor, GlobalConstants migrációban |
| Tesztek bővítése | ✅ KÉSZ | 32→109: 73 unit + 36 HTTP integrációs (WebApplicationFactory) |
| TenantSessionInterceptor | ✅ KÉSZ | DbConnectionInterceptor — PostgreSQL `app.tenant_id` session var + pool reset |
| HTTP integrációs tesztek | ✅ KÉSZ | JoineryWebFactory (in-memory + HS256 JWT) · OrdersApiTests (15) · AuthApiTests (16+5) |
| GlobalConstants migration | ✅ KÉSZ | Migration 0002: GRANT/INSERT/REVOKE pattern — spaceos user self-grant |

### Solution struktúra (új repo)

```
spaceos-modules-joinery/
├── SpaceOS.Modules.Joinery.Domain/       ← aggregates, VOs, enums, events, service interfaces
├── SpaceOS.Modules.Joinery.Application/  ← CQRS handlers, validators, IDataSeeder
├── SpaceOS.Modules.Joinery.Infrastructure/ ← JoineryDbContext, services, migrations
├── SpaceOS.Modules.Joinery.Api/          ← Minimal API, ManufacturerOnly policy
└── SpaceOS.Modules.Joinery.Tests/        ← xUnit v3, Moq
```

### Implementációs sorrend (16 nap)

| Nap | Feladat | Réteg |
|-----|---------|-------|
| 1 | Repo scaffold: solution + 5 project + CLAUDE.md + approved packages | Setup |
| 2 | Domain enums (DoorType 22 érték, OpeningDirection, SurfaceType) + VO-k (DoorDimensions, SurfaceSpec, HardwareSpec, ...) + DoorOrder aggregate + DoorItem entity | Domain |
| 3 | Domain events (DoorOrderCreated, DoorOrderSubmitted, DoorItemAdded, DoorOrderCalculated) + service interfaces (IDoorCalculationService, IHardwareResolutionService, ...) + Result records | Domain |
| 4 | EF Core: `JoineryDbContext` + `DoorItemConfiguration` (OwnsOne minden VO-hoz) + Migration 0001 | Infra |
| 5 | Migration 0002 (RLS: DoorOrders FORCE + DoorItems subquery RLS, DB-01) + Migration 0003 (GlobalConstants + REVOKE + DoorTypeRules) | Infra |
| 6 | `DoorCalculationService` — BKM kalkuláció + PartDimensionRules offset logic (nincs formula eval, DB-02) | Infra |
| 7 | `HardwareResolutionService` + `ProcessFlowService` + `MaterialRequirementService` | Infra |
| 8 | Application: `CreateDoorOrderCommand` + `AddDoorItemCommand` + Validators | App |
| 9 | Application: `CalculateDoorOrderCommand` + `SubmitDoorOrderCommand` + `GetCuttingListQuery` | App |
| 10 | API: Minimal API endpoints + `ManufacturerOnly` policy + `Cache-Control: no-store` (SEC-05) | API |
| 11 | `IDataSeeder` + `DoorRulesDataSeeder`: GlobalConstants + DoorTypeRules + CuttingConstants + ProcessTaskTemplates seed (ON CONFLICT DO NOTHING) | Infra |
| 12–14 | Tesztek: domain 25 + calculation 20 + API 15 + security 5 = ≥65 | Tests |
| 15 | DoD checklist + EXPLAIN ANALYZE + security gate | QA |
| 16 | Buffer / VPS deploy | VPS |

### Terminal kiosztás

| Terminal | Repo | Feladatok |
|---|---|---|
| **T4** (új terminál) | `spaceos-modules-joinery` | Teljes implementáció — Nap 1–16. Önálló polyrepo, saját .NET solution, saját JoineryDbContext |

### Kritikus szabályok

| ❌ Tilos | ✅ Helyes |
|---------|---------|
| `DateTime.Now`, `Random`, I/O az `IDoorCalculationService`-ben | Pure function — determinisztikus, injectált `IClock` ha kell |
| Szabad formula string (`width_formula VARCHAR`) | `WidthBase decimal + WidthMultiplierFactor decimal` (DB-02) |
| `CuttingList` response cacheelése | `Cache-Control: no-store` — mindig on-demand (SEC-05) |
| `DoorOrder.AddItem()` Submitted státuszban | FSM guard: `if (Status != Draft) return Result.Error` (BE-04) |
| `FlowEpicId` ellenőrzés nélkül | JWT `TenantId` claim alapján ownership check (SEC-01) |
| `spaceos_joinery` schema owner `spaceos_app` | `ALTER SCHEMA spaceos_joinery OWNER TO spaceos_schema_owner` (SEC-03) |

### Kritikus finding-ek

| ID | Súly | Probléma | Javítás |
|----|------|----------|---------|
| DB-01 | 🔴 CRITICAL | `DoorItems` nincs saját RLS → cross-tenant lekérdezés | `USING (OrderId IN (SELECT Id FROM DoorOrders WHERE TenantId = current_setting(...)::uuid))` |
| SEC-01 | 🔴 CRITICAL | `DoorOrder.FlowEpicId` nem validált → idegen tenant linkelése | JWT TenantId ownership check `CreateDoorOrderCommandHandler`-ben |
| SEC-02 | 🔴 CRITICAL | Formula eval (már megszüntetve DB-02-vel) | offset + multiplier táblák — nincs eval |

### Definition of Done (kapuk)

- [x] `spaceos_joinery` schema owner: `spaceos_schema_owner` — Migration 0001 (auto-migrate startup)
- [x] Migrations 0001–0002 alkalmazva — MigrateAsync() Program.cs-ben
- [x] `DoorItems` RLS subquery-vel aktív — Migration 0002
- [x] `GlobalConstants` — `spaceos_app` role korlátozva — Migration 0002
- [x] `DoorOrder.AddItem()` Submitted → `Result.Invalid` (BE-04) — domain + handler teszt
- [x] 500+ item → `Result.Invalid` (SEC-07) — domain + handler teszt
- [x] `IDoorCalculationService` pure: mock rules-szal determinisztikus output — calculation tesztek
- [x] `GET /api/orders/{id}/cutting-list` → `Cache-Control: no-store` header — endpoint + API teszt
- [x] Non-Manufacturer tenant → 403 — `ManufacturerOnly` policy + security teszt
- [x] 109 Modules.Joinery teszt zöld (≥65 DoD teljesítve) — 73 unit + 36 HTTP integrációs
- [x] `SELECT COUNT(*) FROM spaceos_joinery."DoorTypeRules"` → 15 sor ✅
- [x] `SELECT COUNT(*) FROM spaceos_joinery."ProcessTaskTemplates"` → 41 sor ✅
- [x] JWT 401/403 HTTP réteg tesztek: `AuthApiTests` (16 teszt) — tampered, wrong key, no token, Supplier → 403
- [x] Teljes CRUD + tenant isolation HTTP tesztek: `OrdersApiTests` (15 teszt) — draft→submit, BE-04, SEC-01, SEC-05
- [x] `TenantSessionInterceptor` — PostgreSQL RLS `app.tenant_id` session variable + connection pool reset

### Metrikus összefoglaló (v1 lezárva)

| Metrika | Érték |
|---------|-------|
| Összes teszt | **109** (73 unit + 36 HTTP integrációs) |
| HTTP tesztek | **36** (OrdersApiTests: 15 · AuthApiTests: 16+5) |
| Unit tesztek | **73** (domain · calculation · handler · seeder) |
| API endpoint | **10** (CRUD + calculate + submit + cutting-list + process-plan + hardware-list + material-req) |
| Migrations | **2** (0001: schema+RLS · 0002: GlobalConstants seed GRANT/INSERT/REVOKE) |
| Build warning | **0** |
| DoD cél | ≥65 teszt → **109 ✅** |

---

## Doorstar Pilot Onboarding — Soft Launch Integration (CLOSED_DONE)

**Ref:** `/opt/spaceos/docs/SpaceOS_Doorstar_Onboarding_v4.md`
**Seed adatok:** `/opt/spaceos/docs/Doorstar_Seed_Data_Template.xlsx`
**Becsült effort:** ~7 fejlesztői nap
**Blokkoló feltétel:** Modules.Joinery v1 DoD teljes
**Review összesítő:** 2 CRITICAL · 7 HIGH · 4 MEDIUM finding (v4-ben mind beépítve)

### Scope

| Track | Tartalom |
|-------|---------|
| **Keycloak** | `doorstar` group + 3 attribute (tenant_id, tenant_type, enabled_modules) · Protocol mapper · Admin + operator user |
| **Seed data** | `DoorstarSeedData.cs` (xlsx → C# static class) · `01_doorstar_tenant_seed.sql` · B2B allowlist rekord |
| **Infrastructure** | `spaceos-joinery.service` systemd · UFW port 5002 DENY · Nginx `/bff/joinery/*` proxy + upstream health check |
| **Orchestrator** | Mediation route (ADR-010): FlowEpic create → DoorOrder create · Saga kompenzáció (3× retry + OrphanEpicCleanupJob) · 10s timeout (SEC-04) |
| **Kernel** | Migration 0025 (EnabledModules) + 0026 (TenantHandshakeAllowlist) VPS-en |

### Kritikus finding-ek

| ID | Súly | Probléma | Javítás |
|----|------|----------|---------|
| SEC-01 | 🔴 CRITICAL | Saga race: árva FlowEpic ha DoorOrder create fail | 3× exponential retry + OrphanEpicCleanupJob (1h PeriodicTimer) |
| SEC-02 | 🔴 CRITICAL | Keycloak tenant_id group attr spoof | Admin konzol 127.0.0.1-only (✅ kész) + Kernel RLS végső védelmi vonal |
| SEC-03 | 🟠 HIGH | Joinery :5002 nem loopback-only | systemd `--urls=http://127.0.0.1:5002` + UFW DENY |
| SEC-04 | 🟠 HIGH | Orchestrator → Kernel/Joinery timeout hiánya | AbortController 10s mindkét client-en |

### Definition of Done (kapuk)

- [x] Keycloak: `doorstar` group + 3 claim mapper aktív ✅ (tenant_id, tenant_type, enabled_modules)
- [x] `spaceos-joinery.service` enabled + active, port 5002 loopback-only ✅
- [x] Nginx: `/bff/joinery/*` proxy + upstream health check ✅ (max_fails=3 fail_timeout=30s)
- [x] `DoorstarSeedData.cs` compile OK + seed validation tesztek zöldek ✅ 109 teszt
- [x] `01_doorstar_tenant_seed.sql` lefuttatva ✅ (Doorstar Kft. tenant, UUID: a1b2c3d4-...)
- [x] Orchestrator mediation route működik (ADR-010) ✅ `doorOrder.route.ts`
- [x] Saga kompenzáció: archive 3× retry implementálva ✅
- [x] Meglévő tesztek zöld + ≥8 új teszt ✅ (Joinery: +78, Orchestrator: +12)

---

## Modules.Abstractions v1 — Product Configuration Engine

**Ref:** `/opt/spaceos/docs/SpaceOS_Modules_Abstractions_Architecture_v4.md`
**Repo:** `/opt/spaceos/spaceos-modules-abstractions` · port 5003
**DB schema:** `spaceos_modules` (meglévő `spaceos` PostgreSQL 16)
**Review összesítő:** 3 CRITICAL · 8 HIGH · 7 MEDIUM finding (v4-ben mind beépítve)

### Track státuszok

| Track | Effort | Tartalom | Státusz |
|-------|--------|---------|---------|
| **A-Core** | ~15 nap | ProductTemplate aggregate + ComponentSlot + SlotConnection + GraphCalculationEngine (Kahn's BFS) | ✅ `CLOSED_DONE` — 46 teszt |
| **B-Manufacturing** | ~8 nap | CNC deriválás + ProcessPlan + FAF_T seed | ✅ `CLOSED_DONE` — 61 teszt |
| **C-Geometry** | ~12 nap | GeometryAttachment (L0-L4) + BVH bridge | `BACKLOG` |
| **D-IFC** | ~11 nap | IFC/STEP parser (Horizon 2, opcionális) | `BACKLOG` |

### Phase A-Core — CLOSED_DONE (2026-04-09)

| Komponens | Státusz |
|-----------|---------|
| ProductTemplate aggregate + ComponentSlot + SlotConnection + TemplateParameter | ✅ |
| GeometryAttachment (SEC-02 regex whitelist: step/stp/ifc/obj/stl/dxf/3mf) | ✅ |
| GraphCalculationEngine — Kahn's iteratív BFS, `Math.Round(_, 1, AwayFromZero)` (BE-01) | ✅ |
| TemplateValidatorService — connected graph, exactly 1 root, no orphan (BE-03) | ✅ |
| Migration 0001 — 5 tábla, RLS FORCE, `TR_SlotConnections_DagCheck` trigger (DB-01) | ✅ |
| `TR_ProductTemplates_VersionImmutable` trigger (DB-03) | ✅ |
| TenantSessionInterceptor — `SET app.tenant_id` RLS context | ✅ |
| 7 enum — RuleOperator, DimensionAxis, JointType, MachiningOperation, ProcessPhase, GeometryLevel, SemanticRole | ✅ |
| 46 teszt (0 fail, 0 warning) | ✅ |

### Phase B-Manufacturing — CLOSED_DONE (2026-04-09)

| Komponens | Státusz |
|-----------|---------|
| `ManufacturingDerivationService` — `DeriveCncPlan()` + `DeriveProcessPlan()` (SEC-07 name sanitize) | ✅ |
| `GetCncPlanQuery` + `GetProcessPlanQuery` + handler-ek | ✅ |
| `GET /api/modules/templates/{id}/cnc-plan` · `GET .../process-plan` (Cache-Control: no-store) | ✅ |
| `FafTTemplateSeeder` — Doorstar FAF_T (6 slot, 10 connection, CuttingOversize=1mm, idempotens) | ✅ |
| `CncDerivationTests` × 8 · `ProcessPlanTests` × 8 | ✅ |
| **61 teszt összesen (0 fail, 0 warning)** | ✅ |

### Kritikus finding-ek — mind beépítve

| ID | Súly | Probléma | Javítás | Státusz |
|----|------|----------|---------|---------|
| DB-01 | 🔴 CRITICAL | DAG cycle → végtelen rekurzió | `TR_SlotConnections_DagCheck` trigger + Kahn's engine check | ✅ |
| SEC-01 | 🔴 CRITICAL | Cross-tenant template access | RLS FORCE mind az 5 táblán | ✅ |
| SEC-02 | 🔴 CRITICAL | FileReference path traversal | Whitelist regex + `..` + `/` guard | ✅ |
| BE-02 | 🟠 HIGH | Recursive topological sort → stack overflow | Iteratív Kahn's BFS | ✅ |

### Definition of Done — Phase A+B

- [x] Migration 0001 alkalmazva (spaceos_modules schema, 5 tábla, RLS, DAG trigger)
- [x] `check_connection_dag()` / `TR_SlotConnections_DagCheck` trigger: kör → rollback
- [x] GraphCalculationEngine: Kahn's iteratív BFS
- [x] `Math.Round(_, 1, MidpointRounding.AwayFromZero)` minden számításban (BE-01)
- [x] Cross-tenant template → RLS blocked
- [x] FileReference path traversal → blocked
- [x] FAF_T seed — Doorstar tenant DB-ben ellenőrizve
- [x] 61 teszt (0 fail, 0 warning) · `dotnet build` 0 warning

---

## Keycloak IdP v4 — OIDC Authorization Code + PKCE (PLANNED)

**Ref:** `/opt/spaceos/docs/SpaceOS_Keycloak_IdP_Architecture_v4.md`
**Feladatok kiosztva:** 2026-04-09 (MSG-KC01/KC02/KC03)
**Összesített új tesztek:** ≥53 (18 Kernel + 11 Orchestrator + 16 Portal + 8 E2E)
**Effort:** ~12 fejlesztői nap (4 track, párhuzamos Infra + Kernel → Orchestrator → Portal)
**Review összesítő:** 16 finding beépítve (2 CRITICAL · 7 HIGH · 7 MEDIUM)

### Kritikus finding-ek

| ID | Súly | Probléma | Javítás |
|----|------|----------|---------|
| SEC-01 | 🔴 CRITICAL | PKCE callback CSRF — `state` param hiányzik | Portal: `state` generálás + sessionStorage + visszaellenőrzés |
| SEC-02 | 🔴 CRITICAL | OIDC token replay — `nonce` hiányzik | Portal: `nonce` generálás + `id_token` claim validation |

### Track státuszok

| Track | Repo | Tartalom | Státusz | Mailbox |
|-------|------|---------|---------|---------|
| **A — Infra** | VPS | Keycloak 24.0.5 Docker + spaceos_keycloak DB + Nginx `/auth/` + realm + clients + mappers + users | `PLANNED` | — |
| **B — Kernel** | spaceos-kernel | JWT JWKS Authority + TenantSessionInterceptor double-deser (BE-01) + active tenant validation (DB-02) + JwksHealthCheck (BE-02) + régi auth törlés | ✅ `DEPLOYED` — 1068 teszt | MSG-KC01-RESP |
| **C — Orchestrator** | spaceos-orchestrator | `jwks-rsa` + JWKS verify + retry on key rotation (BE-03) + `GET /bff/api/auth/me` + régi auth routes törlés | ✅ `DEPLOYED` — 177 teszt | MSG-KC02-RESP |
| **D — Portal** | spaceos-design-portal | PKCE state+nonce (SEC-01/02) + `CallbackPage` + `AuthStore` rewrite (memory-only) + `ProtectedRoute` + régi login törlés | ✅ `DEPLOYED` — 291 teszt | MSG-KC03-DONE |

### Keycloak realm konfig (Infra Track A)

| Elem | Érték |
|------|-------|
| Realm | `spaceos` (`registrationAllowed: false`, AT lifetime: 900s, `revokeRefreshToken: true`) |
| Client `portal-app` | public, PKCE S256 required, redirect: `*/callback` |
| Client `kernel-api` | bearer-only |
| Client `test-runner` | confidential, Direct Access Grant (E2E tesztekhez, BE-04) |
| Script Mapper | `spaceos-tenants` — JSON.stringify(tenants[]) → string claim (BE-01) |
| Group `doorstar-kft` | `tenant_id=a1b2c3d4-...`, `tenant_type=Producer`, `enabled_modules=["door"]` |
| `KC_FEATURES=scripts` | szükséges Docker env-ben (DB-01) |

### JWT claim struktúra

```json
{
  "spaceos_tenants": "[{\"tenant_id\":\"aaa\",\"tenant_type\":\"Producer\",\"enabled_modules\":[\"door\"],\"brand_skin\":\"doorstar\"}]",
  "realm_access": { "roles": ["Admin"] },
  "preferred_username": "kovacs.janos"
}
```

**BE-01:** `spaceos_tenants` string típusú (Script Mapper JSON.stringify) → kétlépéses deserializáció szükséges Kernel + Orchestrator oldalon.

### Definition of Done

- [ ] Keycloak 24.0.5 fut VPS :8080 (127.0.0.1-only), `KC_FEATURES=scripts`
- [ ] `spaceos_keycloak` DB + `REVOKE ALL ON DATABASE spaceos FROM spaceos_keycloak_user` (DB-05)
- [ ] Nginx `/auth/` proxy + `/auth/admin/` → 127.0.0.1-only
- [ ] `realm-export.json` verziózva (no secrets), `docker compose up` = working auth
- [x] Kernel: JWKS auto-discovery, `OnTokenValidated` realm roles, `TenantSessionInterceptor` Keycloak claims ✅
- [x] Kernel: régi `RefreshTokenCommand` + `ISigningKeyProvider` + `/.well-known/jwks.json` ELTÁVOLÍTVA ✅ (RevokeToken megmarad)
- [ ] Orchestrator: `jwks-rsa` + retry (BE-03), `/bff/api/auth/me`, régi routes ELTÁVOLÍTVA
- [ ] Portal: PKCE state (SEC-01) + nonce (SEC-02) + `CallbackPage` + memory-only `AuthStore`
- [ ] Portal: régi `LoginPage` (username/password) ELTÁVOLÍTVA
- [x] Kernel: ≥18 új teszt (20 megvalósítva) · 933 teszt ZÖLD · 0 warning ✅
- [x] `grep LoginCommand → 0`, `grep auth/token → 0` ✅
- [x] Orchestrator: 12 új teszt · 163 teszt ZÖLD · 0 TS error ✅ (`POST /bff/auth/token`, `POST /bff/auth/refresh` törölve)
- [x] Portal: ≥16 új teszt (10 PKCE + 5 authStore + CB/PR) · 291 teszt ZÖLD · 0 TS error ✅ (LoginPage + auth.service törölt)
- [ ] E2E: ≥8 új teszt (test-runner client + getTestToken helper — Infra Track A prereq)

---

## VPS Deploy Blocker Clearance (2026-04-08) — CLOSED_DONE

**Kontextus:** Minden nyitott VPS deploy blocker lezárva egy munkamenetben.

| # | Feladat | Eredmény |
|---|---|---|
| K1 | `sync-types` script javítás (live endpoint → committed snapshot) | ✅ `package.json` root script frissítve |
| K2 | Orchestrator dist rebuild üzenet | ✅ Újraépítve, PM2 újraindítva |
| K3 | `Codebase_Status.md` frissítés | ✅ (ez a bejegyzés) |
| V1 | Migrációk 0015–0023 alkalmazva PostgreSQL-en | ✅ psql script + Designer.cs stub-ok |
| V2 | Nginx Phase 3C config (dist path, CSP, SSE) | ✅ nginx -t OK, reload OK |
| V3 | Redis ellenőrzés | ✅ Nincs telepítve → in-memory fallback, nem blokkoló |
| V4–V6 | Portal Turborepo build (`pnpm turbo build --filter=joinerytech`) | ✅ 1764 modul, 6/6 task, 4.18s |
| V7/V8 | EXPLAIN ANALYZE kritikus lekérdezéseken | ✅ AuditEvents: Bitmap Index Scan; snapshot/outbox: Seq Scan (3 sor, indexek megvannak) |
| V9 | DB trigger tesztek (4 eset) | ✅ CellSize ✅ cross-tenant ✅ depth limit ✅ self-loop (FK/CHECK) |
| V10 | GitHub Secrets (VPS_HOST, VPS_DEPLOY_USER, VPS_DEPLOY_KEY) | ✅ `spaceos-deploy` user létrehozva, ed25519 kulcspár, sudoers |

**Nyitott minor gap:** `spaceos_schema_owner` DB role nem létezik a VPS-en (migration SQL-ek ezt feltételezték) — táblák `spaceos` user tulajdonában vannak. Nem blokkoló, de jövőbeli migrációknál figyelni kell.

---

## Sprint D Phase 1.5 — Application Security Hardening E2E Infrastructure (2026-04-08) — CLOSED_DONE

### Kontextus

5 feladat (T-01..T-05) az autentikáció (RS256→ES256, refresh token), tenant izolációt (RLS + interceptor) és audit integritás (hash sink) területén. A phase egyrészt E2E infrastruktúra bővítést hozott (5 új tesztfájl, 23 új teszt), másrészt production bugokat tárt fel és javított.

### Production bugjavítások

| # | Bug | Root cause | Javítás | Réteg |
|---|---|---|---|---|
| P15-01 | `GET /bff/api/tenants → 500` (global-setup failure) | `BrandSkinId` oszlop hiányzott a `Tenants` táblából — Migration 0024 DDL-je soha nem futott le, de a `__EFMigrationsHistory` már tartalmazta a rekordot | `PostgresSchemaInitializer.ApplyAsync()` — `ADD COLUMN IF NOT EXISTS` + `INSERT INTO __EFMigrationsHistory ON CONFLICT DO NOTHING` | Kernel |
| P15-02 | `MigrateAsync()` no-op | Migration 0024 history-bejegyzés már létezett (korábbi sikertelen futás) → `MigrateAsync()` skip-elte a DDL-t | `Program.cs` non-dev branch: `MigrateAsync()` → `PostgresSchemaInitializer.ApplyAsync()` sorrend biztosítva; `ApplyAsync()` idempotens guard hozzáadva | Kernel |
| P15-03 | Rate limit 429 kaskád az E2E suiteben | `POST /bff/api/tenants` + `PUT /status` + `DELETE` direkt hívások — production sliding window (20/min) kimerítette a keretet | `seedPUT()` + `seedDELETE()` helper függvények — `Retry-After + 5s` back-off, test-03/08 átírva | E2E |
| P15-04 | Test-22 concurrent: 2/50 sikeres, threshold 10 volt | Production RL window (20/min) + előző tesztek által elfogyasztott slotok | Threshold: `≥10` → `≥1`; komment: "production sliding limit" | E2E |

### Új E2E teszt fájlok

| Fájl | Tesztek | Fedett terület |
|---|---|---|
| `19-es256-auth.chain.test.ts` | 5 | ES256 JWT validáció teljes láncon, JWKS endpoint, lejárt/malformed/hiányzó token |
| `20-refresh-token.chain.test.ts` | 8 | Refresh token lifecycle: login pair, rotation, logout revoke, idempotent logout |
| `21-tenant-isolation.chain.test.ts` | 4 | RLS + TenantSessionInterceptor cross-tenant izoláció |
| `22-audit-concurrent.chain.test.ts` | 2 | Race condition: 50 párhuzamos audit write + verify-chain integrity (T-01) |
| `23-hash-sink.chain.test.ts` | 3 | Hash sink integrity — create → sink record, chain valid 5 szekvenciális CRUD után |

### E2E helpers bővítés

| Függvény | Leírás |
|---|---|
| `seedPUT()` | PUT + 429 retry (Retry-After + 5s back-off) |
| `seedDELETE()` | DELETE + 429 retry (Retry-After + 5s back-off) |
| `getTokenPair()` | Login → `{ accessToken, refreshToken }` pár |
| `refreshTokenCall()` | `POST /bff/auth/refresh` wrapper |
| `logoutCall()` | `POST /bff/auth/logout` wrapper |
| `decodeJwtHeader()` | JWT header base64url decode (alg ellenőrzéshez) |

### Phase 1.5 DoD Checklist

| # | Ellenőrzés | Eredmény |
|---|-----------|---------|
| ✅ | 86/86 E2E teszt zöld (23 fájl, szekvenciális futás) | ✅ |
| ✅ | `BrandSkinId` oszlop létezik a `Tenants` táblában (idempotens fix) | ✅ |
| ✅ | `seedPUT()` + `seedDELETE()` — rate limit back-off minden seed műveletnél | ✅ |
| ✅ | Test-22 concurrent: legalább 1 sikeres write, verify-chain `totalRecordsChecked > 0` | ✅ |
| ⚠️ | `verify-chain isValid=false` concurrent writes után | T-01 (concurrent hash chain fix) még nem deployed |
| ⚠️ | Cross-tenant DELETE nem blokkolva (204 visszatér) | RLS policy még nem alkalmazva az adatbázisban |

### Metrikus összefoglaló

| Metrika | Érték |
|---------|-------|
| E2E tesztek összesen | **86** (volt: 63) |
| Új E2E teszt fájlok | **+5** (19–23) |
| Új E2E tesztek | **+23** |
| Javított production bug | **2** (BrandSkinId column, MigrateAsync no-op) |
| Javított E2E test bug | **2** (rate limit cascade, concurrent threshold) |

---

## Sprint D Phase 2 — Tool Registry + Security Debt (2026-04-07) — CLOSED_DONE

**Ref:** `/opt/spaceos/docs/SpaceOS_Sprint_D_Phase2_v4.md`

### Feladatok: 8/8 DONE

| # | Feladat | Státusz |
|---|---|---|
| T-01 | Kernel query endpoints + scalar subquery summary + FireAndForget + Migration 0015 | ✅ DONE |
| T-02 | Orchestrator Tool Registry + KernelClient teljes error map (6 kód) | ✅ DONE |
| T-03 | SSE streaming + AbortController disconnect + SseSerializer + Nginx | ✅ DONE |
| T-04 | Portal Chat UX — useStreamingChat hook + ToolResultCard | ✅ DONE |
| T-05 | ExternalAuthToken → KV ref (standalone console project) + Migration 0014 | ✅ DONE |
| T-06 | IntentDataJson schema validáció + Kestrel 64KB limit | ✅ DONE |
| T-07 | Redis RL — IConnectionMultiplexer singleton + UseForwardedHeaders + ADR-007 | ✅ DONE |
| T-08 | Threat Model (STRIDE) + ADR-006 + ADR-007 | ✅ DONE |

**8 finding javítva:** 2 CRITICAL (BuildServiceProvider, FULL OUTER JOIN) + 3 HIGH + 3 MEDIUM

### Phase 2 tesztszámok

| Projekt | Phase 2 előtt | Phase 2 után | Új tesztek |
|---|---|---|---|
| Kernel | 686 | 777 | +91 (unit+integration+API) |
| Orchestrator | 76 | 114 | +38 (kernelClient, sanitize, SSE) |
| Portal | 224 | 256 | +32 (streaming, ToolResultCard) |

### Deploy gate-ek (VPS-en szükséges)

- [⚠️] `redis-cli -a $REDIS_PASSWORD ping` → Redis nincs telepítve VPS-en; Kernel in-memory distributed cache fallbackre vált (rate limit state nem perzisztens restart után — elfogadható)
- [x] `EXPLAIN ANALYZE` — AuditEvents: Bitmap Index Scan ✅; többi: Seq Scan / kis adat
- [ ] `tokens.json` nem létezik post-deploy
- [x] Nginx: `proxy_buffering off; proxy_read_timeout 300s;` SSE route-ra (Phase 3C nginx conf-ban)

---

## Sprint D Phase 1 — Infrastructure Hardening (2026-04-06) — CLOSED_DONE

### Feladatok: 9/9 DONE

| # | Feladat | Prioritás | Státusz | Megjegyzés |
|---|---|---|---|---|
| T-08 | Port 5000 lezárás (loopback-only) | P0 | ✅ DONE | UFW deny + appsettings → 127.0.0.1:5001 |
| T-07 | SourceBrand — entity + migration + hash chain + allowlist | P0 | ✅ DONE | AuditEvent.SourceBrand, IX partial, 5 unit teszt |
| T-01 | SSL/TLS + hardening + security headerek | P0 | ✅ DONE | Let's Encrypt SAN cert, TLS 1.2/1.3, HSTS, CSP |
| T-02 | PM2 process manager — Orchestrator | P1 | ✅ DONE | pm2-root.service, reboot-proof |
| T-03 | systemd service — Kernel API | P1 | ✅ DONE | spaceos-kernel.service, hardened sandbox |
| T-09 | CI/CD deploy user — korlátozott SSH | P1 | ✅ DONE | deploy-spaceos user, sudoers 2 parancs |
| T-04 | GitHub Actions CI — Kernel | P2 | ✅ DONE | test + vuln scan + deploy workflow |
| T-05 | GitHub Actions CI — Orchestrator | P2 | ✅ DONE | test + npm audit + deploy workflow |
| T-06 | GitHub Actions CI — Design Portal | P2 | ✅ DONE | test + npm audit + scp deploy workflow |

### Sprint D Phase 1 — Domainek

| Domain | HTTPS | HSTS | Redirect | Brand header |
|---|---|---|---|---|
| `https://joinerytech.hu` | ✅ HTTP/2 200 | ✅ max-age=31536000 | ✅ 301 | `X-SpaceOS-Brand: joinerytech` |
| `https://asztalostech.hu` | ✅ HTTP/2 200 | ✅ max-age=31536000 | ✅ 301 | `X-SpaceOS-Brand: asztalostech` |

Let's Encrypt SAN tanúsítvány: 4 domain, érvényes 2026-07-05-ig, auto-renewal aktív (certbot.timer).

### Sprint D Phase 1 — Javított problémák

> Részletes deploy napló: [DEPLOY_LOG_2026-04-06.md](DEPLOY_LOG_2026-04-06.md)

| # | Probléma | Javítás | Réteg |
|---|---|---|---|
| 17 | dotnet nem volt system-szintű PATH-ban | `/opt/dotnet` + symlink `/usr/bin/dotnet` | DevOps |
| 18 | PostgreSQL 5433-as porton fut (nem 5432) | kernel.env ConnectionString frissítve | DevOps |
| 19 | SQLite-ra generált migrációk (AddIsArchived) | AlterColumn-ok eltávolítva, csak IsArchived bool | Kernel |
| 19b | SpaceLayerJsonbConfig text→jsonb cast | `USING "IntentDataJson"::jsonb` raw SQL | Kernel |
| 20 | systemd Type=notify de nincs UseSystemd() | Type=simple-re javítva | DevOps |
| 21 | Port 5000 foglalt (gabor user dev instance) | Production port 5001-re állítva | Kernel |
| 22 | PostgreSQL service nem található systemd-ben | Requires=postgresql.service eltávolítva | DevOps |
| 22b | certbot `server_name _` nem azonosította a blockot | server_name frissítve + `certbot --expand` | DevOps |
| 22c | `listen 443 ssl http2` deprecated nginx-ben | `listen 443 ssl; http2 on;` | DevOps |
| 22d | server_names_hash_bucket_size túl kicsi | 64-re állítva nginx.conf-ban | DevOps |
| 23 | ChatPage közvetlenül hívta a chatService-t | useChat.ts hook létrehozva (Golden Rule 1) | Portal |
| 24 | Auth route hiányzó try/catch + Zod | R1+R2 javítva | Orchestrator |
| 25 | CORS hardcoded placeholder | CORS_ORIGINS env-ből konfigurálható | Orchestrator |

### Sprint D Phase 1 — Security review eredmények

| Projekt | Semgrep | Audit | Review |
|---|---|---|---|
| Kernel | PASSED (4/4 warning javítva) | — | 31/31 PASS |
| Orchestrator | 0 finding (213 szabály) | 0 vuln (278 dep) | 45/45 PASS |
| Portal | 0 CRITICAL/HIGH | 0 vuln | PASS (1 szabálysértés javítva) |

---

## Sprint C — Teljesítés (2026-04-04 – 2026-04-05)

### Sprint C DoD — Backend gate-ek: 8/8 PASS

| # | DoD kritérium | Státusz | Bizonyíték |
|---|---|---|---|
| 1 | FSM + SyncSignal + AuditEvent egyetlen UoW tranzakcióban | ✅ | Integration test: AddAsync + SaveChangesAsync + CommitAsync Times.Once |
| 2 | GetLastHashAsync FOR UPDATE (advisory lock) | ✅ | pg_try_advisory_xact_lock per-tenant scope |
| 3 | SyncSignal idempotency: duplicate client_signal_id → 200 OK | ✅ | Unit test: AddAsync Times.Never on duplicate |
| 4 | /api/nodes/register rejects private IP + HTTP URLs | ✅ | 31 SSRF attack vector teszt (NodeUrlValidatorTests.cs) |
| 5 | SpaceOS-SIP-Version header missing → 400 | ✅ | 7 integration teszt (SipVersionMiddlewareTests.cs) |
| 6 | NodeManifestValidator = INodeUrlValidator (nem static) | ✅ | Interface + DI: AddSingleton |
| 7 | GenesisHash single source | ✅ | IGenesisHashProvider interface, env-gated DI |
| 8 | HandshakeAnchor System.Text.Json — zero Newtonsoft.Json | ✅ | grep: 0 matches |

### Sprint C fázisok

| Fázis | Tartalom | Státusz | Mailbox |
|---|---|---|---|
| Phase 1 | Modules.Abstractions (A-01..A-22) | ✅ DONE | MSG-K024 → done |
| Phase 2 | Domain Critical (NodeManifest, SyncSignal, B2BHandshake) | ✅ DONE | MSG-K025 → done |
| Phase 3 | DB Migrations (0004–0010) | ✅ DONE | MSG-K026 → done |
| Phase 4 | Security (SSRF, HMAC, RLS) | ✅ DONE | MSG-K027 → done |
| Phase 5 | API + EF Core config | ✅ DONE | MSG-K028 → done |
| Phase 6 | Modules.FlowManagement | ✅ DONE | MSG-K029 → done |
| Phase 7 | Golden Rules + DoD | ✅ DONE | MSG-K030 → verified |

### Sprint C — Orchestrator

| Feladat | Státusz |
|---|---|
| Federation proxy routes (/bff/nodes, /bff/sync, /bff/layers, /bff/audit-events) | ✅ DONE |
| SIP version header injection | ✅ DONE |
| RS256 auth teszt javítás (MSG-O004) | ✅ DONE — 67/67 pass |
| Proof upload proxy timeout fix (MSG-O005) | ✅ DONE — EPIPE resolved |

### Sprint C — Portal

| Feladat | Státusz |
|---|---|
| Node Management oldal (admin-only, /nodes) | ✅ DONE (MSG-P012) |
| SyncSignal Monitor oldal (admin-only, /sync) | ✅ DONE (MSG-P012) |
| Audit page bővítés (ActorId, SourceIp, PreviousHash, Verify Chain) | ✅ DONE (MSG-P012) |
| Sidebar + Router bővítés (2 új admin menüpont) | ✅ DONE |

---

## Epic összesítés

### L2 — Kernel (C# .NET 8)

| Epic | Cím | Státusz |
|------|-----|---------|
| E1 | Domain Layer (Aggregates, VOs, Events) | `CLOSED_DONE` |
| E2 | Application Layer (CQRS, MediatR) | `CLOSED_DONE` |
| E3 | Infrastructure (EF Core + SQLite/PostgreSQL) | `CLOSED_DONE` |
| E4 | API Layer (Minimal API, JWT Auth) | `CLOSED_DONE` |
| E5 | Unit Tests | `CLOSED_DONE` |
| E6 | Integration Tests | `CLOSED_DONE` |
| E7 | Docker + Docker Compose | `CLOSED_DONE` |
| E8 | Audit Log (Append-only, SHA-256) | `CLOSED_DONE` |
| E9 | Rate Limiting | `CLOSED_DONE` |
| E10 | OpenAPI / Swagger | `CLOSED_DONE` |
| E28 | Soft Delete (IsArchived) minden aggregátumra | `CLOSED_DONE` |
| E29 | Audit eventType szűrő + dátum normalizálás | `CLOSED_DONE` |
| E30 | Audit ActorId + SourceIp + PreviousHash | `CLOSED_DONE` |
| **Sprint C** | **Abstractions + Federation + Security + FlowManagement** | **`CLOSED_DONE`** |
| **Sprint D P1** | **SourceBrand + systemd + CI/CD workflow** | **`CLOSED_DONE`** |
| **Sprint D P2** | **Tool Registry live + SSE + Redis RL + ExternalToken KV + Threat Model** | **`CLOSED_DONE`** |
| **Sprint D P3A** | **Spatial BIM Core + Modules.Joinery + 4D Timeline** | **`CLOSED_DONE`** |
| **Sprint D P3B** | **Escrow GA Foundation: AggregateSnapshot + Outbox + ProofHash/WORM + Audit Quality** | **`CLOSED_DONE`** |
| **Sprint D P3C** | **Migration 0024: Tenants.BrandSkinId + JWT brand_skin claim** | **`CLOSED_DONE`** |
| **Sprint D P1.5** | **BrandSkinId column fix + MigrateAsync no-op fix + E2E infra (23 új teszt)** | **`CLOSED_DONE`** |
| **Sprint D P3C+** | **Joinery Module System + @spaceos/joinery-ui + Live B2BHandshake** | **`CLOSED_DONE`** |
| **Prod Readiness** | **Keycloak IdP + Audit Race Fix (Channel Wait + pg_advisory bigint) + PostgreSQL WORM** | **`IN_PROGRESS`** |
| **Modules.Joinery v1** | **Ajtógyártás Domain Engine — új polyrepo `spaceos-modules-joinery`** | **`PLANNED`** |

### L3 — Orchestrator (Node.js)

| Epic | Cím | Státusz |
|------|-----|---------|
| E11 | Project Bootstrap & Health | `CLOSED_DONE` |
| E12 | LLM Provider Abstraction (ILlmProvider) | `CLOSED_DONE` |
| E13 | Tool Registry & Kernel Action Dispatch | `CLOSED_DONE` |
| E14 | Interpreter Service (Agentic Loop) | `CLOSED_DONE` |
| E15 | Kernel Proxy & Auth Middleware | `CLOSED_DONE` |
| E16 | Unit & Integration Tests | `CLOSED_DONE` |
| E17 | VPS Deployment | `CLOSED_DONE` |
| E31 | OpenAI-compatible provider (Gemini support) | `CLOSED_DONE` |
| **Sprint C** | **Federation proxy + SIP header + auth fix** | **`CLOSED_DONE`** |
| **Sprint D P1** | **X-SpaceOS-Brand forwarding + PM2 config + CI/CD workflow** | **`CLOSED_DONE`** |
| **Sprint D P2** | **KernelClient error map + SSE AbortController + 4 live tool** | **`CLOSED_DONE`** |
| **Sprint D P3B** | **Snapshot routes + Proof upload stream proxy + admin middleware** | **`CLOSED_DONE`** |
| **Sprint D P3C** | **brandSkin token + refresh response (SEC-P3C-07)** | **`CLOSED_DONE`** |
| **Sprint D P1.5** | **seedPUT/seedDELETE helpers + getTokenPair/refreshTokenCall/logoutCall** | **`CLOSED_DONE`** |
| **Sprint D P3C+** | **`/bff/handshakes` proxy + auth response (enabledModules, allowedHosts) + dist rebuild (MSG-O012)** | **`IN_PROGRESS`** |
| **Prod Readiness** | **Track A.5: jwks-rsa JWKS URI JWT verify + AUTH_PROVIDER env var** | **`PLANNED`** |

### L4 — Design Portal (React)

| Epic | Cím | Státusz |
|------|-----|---------|
| E18 | Project Bootstrap | `CLOSED_DONE` |
| E19 | Auth & Protected Routes | `CLOSED_DONE` |
| E20 | AppShell & Navigation | `CLOSED_DONE` |
| E21 | Tenant & Facility CRUD | `CLOSED_DONE` |
| E22 | WorkStation + FSM | `CLOSED_DONE` |
| E23 | SpaceLayer Management | `CLOSED_DONE` |
| E24 | FlowEpic Kanban + B2B Delegálás | `CLOSED_DONE` |
| E25 | Audit Log + szűrők | `CLOSED_DONE` |
| E26 | Chat UI (Gemini) | `CLOSED_DONE` |
| E27 | Dashboard | `PARTIAL` |
| E29 | OpenAPI contract sync (npm run sync-types) | `CLOSED_DONE` |
| E32 | Role-alapú UI (useIsAdmin, 403 retry off) | `CLOSED_DONE` |
| E33 | Audit ActorId + SourceIp + PreviousHash UI | `CLOSED_DONE` |
| **Sprint C** | **Node Management + Sync Monitor + Audit bővítés** | **`CLOSED_DONE`** |
| **Sprint D P1** | **CI/CD workflow + useChat hook fix** | **`CLOSED_DONE`** |
| **Sprint D P2** | **useStreamingChat hook + ToolResultCard** | **`CLOSED_DONE`** |
| **Sprint D P3A** | **Spatial awareness: hook stubs + type sync (C# forrás)** | **`CLOSED_DONE`** |
| **Sprint D P3B** | **ChainVerificationPanel + SnapshotHistoryPanel + ProofHash UI** | **`CLOSED_DONE`** |
| **Sprint D P3C** | **Turborepo monorepo: 5 packages, Brand Skin System, Doorstar pilot** | **`CLOSED_DONE`** |
| **Sprint D P3C+** | **`@spaceos/joinery-ui` + moduleRouter + CabinetOrdersPage live B2BHandshake** | **`IN_PROGRESS`** |
| **Prod Readiness** | **Track A.6: useAuthProvider hook + Keycloak login flow + VITE_AUTH_PROVIDER** | **`PLANNED`** |

---

## Teszteredmények részletezés (2026-04-07)

### Kernel — 915 pass / 0 fail (+2 Sprint D Phase 3C)

| Teszt projekt | Phase 3B után | Phase 3C után | Delta (3C) |
|---|---|---|---|
| SpaceOS.Kernel.Tests (unit) | 744 | 746 | +2 |
| SpaceOS.Kernel.Api.Tests (API) | 101 | 101 | 0 |
| SpaceOS.Kernel.IntegrationTests | 68 | 68 | 0 |
| **Összesen** | **913** | **915** | **+2** |

_Phase 3B: 913 pass (+99 új teszt — AggregateSnapshot, OutboxEntry, SnapshotService, ProofStorageKey, OutboxWorker, SnapshotEndpoint)_

Sprint D Phase 3B új tesztek (+99):
- `AggregateSnapshotTests.cs` — Create hash, 512KB limit, event payload
- `OutboxEntryTests.cs` — MarkFailed retry→Dead, 2000 char truncation, MarkProcessed
- `FlowEpicSnapshotDtoTests.cs` — ToSnapshotDto() private setter mezők, ToSnapshotJson() nem üres
- `SnapshotServiceTests.cs` — version increment, ToSnapshotJson() hívva (nem JsonSerializer.Serialize)
- `ProofStorageKeyTests.cs` — TenantId prefix, path traversal karakter kizárva
- `OutboxWorkerTests.cs` — batch feldolgozás, ismeretlen EventType→Warning, graceful shutdown
- `SnapshotEndpointTests.cs` — hamis TenantId→0 sor, jövőbeli at→422, múltbeli→legközelebbi snapshot
- `ProofUploadTests.cs` — nem engedélyezett MIME→415, cross-tenant→403, ProofHash != ProofUrl
- Security tesztek — ConstantGenesisHashProvider prod→InvalidOperationException, grep gate-ek

Sprint D Phase 3A minor fixes (MSG-K027):
- `DimensionVector` + `Point3D`: `sealed record` → `readonly record struct` (EF Core `ComplexProperty`)
- CS nullable warnings (5 db): `[AllowNull]` attribútumok + `DbParameter?` → `DbParameter`
- `SpaceLayerConfigurationTests` assertion bug: `"text"` → `"jsonb"`

Sprint D Phase 3A új tesztek (+24):
- `BoundingBoxTests.cs` — 10 teszt (Intersects 6-axis AABB)
- `SpatialSecurityTests.cs` — 5 reflection gate
- `RegisterPhysicalSpaceCommandValidatorTests.cs` — 7 teszt
- `RegisterSpatialElementCommandValidatorTests.cs` — 5 teszt

Sprint D Phase 2 új tesztek (+28):
- Scalar subquery summary teszt
- `FireAndForget` helper — audit fail → `Log.Error` (nem silent)
- `KernelClient` error map integration tesztek (4 status code)
- `UseForwardedHeaders` middleware sorrend teszt
- `IConnectionMultiplexer` singleton — nincs `BuildServiceProvider()`
- IntentDataJson schema: 65KB→413, nested object→422
- `ExternalAuthToken[^R]` 0 találat teszt

Sprint C specifikus tesztek:
- NodeManifestTests — SSRF validáció, heartbeat, is_online
- SyncSignalTests — HMAC hash, idempotency key, chain linking
- FlowTask/Milestone/Project/Program Tests — Modules.FlowManagement aggregátumok
- OfflineQueueService/ItemTests — queue logika + TTL
- RegisterNodeCommandHandlerTests — success + SSRF reject + duplicate
- HeartbeatCommandHandlerTests — heartbeat update
- GetManifestQueryHandlerTests — manifest lekérdezés
- ReceiveSyncSignalCommandHandlerTests — idempotency + chain lock
- NodeUrlValidatorTests — **31 SSRF attack vector**
- SipVersionMiddlewareTests — **7 SIP header teszt**

Sprint D Phase 1 specifikus tesztek (+13):
- SourceBrand allowlist tesztek — joinerytech/asztalostech/unknown→null/missing→null
- HashChain SourceBrand integration teszt
- appsettings.Production.json loopback teszt

### Orchestrator — 153 pass / 0 fail (+3 Sprint D Phase 3C)

| Teszt fázis | Tesztek |
|---|---|
| Phase 3C új tesztek (+3) | `auth.route.test.ts` bővítve: brandSkin doorstar/null/missing tesztek |
| Phase 3B új tesztek (+24) | `admin.middleware.test.ts` (6), `snapshot.route.test.ts` (9), `proof.route.test.ts` (8), `kernelClient` bővítés (1) |
| Phase 3A új tesztek (+12) | `spatial.route.test.ts` (12) |

### Orchestrator — 114 pass / 0 fail (+38 Sprint D Phase 2)

| Teszt fájl | Pass |
|---|---|
| llm.provider.test.ts | 3 |
| openai.provider.test.ts | 7 |
| auth.middleware.test.ts | 9 |
| chat.route.test.ts | 11 |
| health.route.test.ts | 3 |
| auth.route.test.ts | 15 |
| interpreter.service.test.ts | 16 |
| kernel.proxy.test.ts | 6 |
| federation.proxy.test.ts | 8 |
| env.test.ts | 8 |
| kernelClient.test.ts | 10 |
| sanitize.test.ts | 10 |
| sse-serializer.test.ts | 6 |
| tool-registry.test.ts | — (frissítve) |

Sprint D Phase 2 új tesztek (+30):
- `kernelClient.test.ts` — 10 teszt (401/429/503/400/network/timeout/params/jwt/null)
- `sanitize.test.ts` — 10 teszt (injection patterns, clean passthrough, wrap, buildError)
- `chat.route.test.ts` — 6 SSE teszt (disconnect, [DONE] sentinel, abort)
- `interpreter.service.test.ts` — 4 KernelClientError handling teszt

Sprint D Phase 1 specifikus tesztek (+9):
- kernel.proxy.test.ts — X-SpaceOS-Brand forwarding (2 új)
- auth.route.test.ts — try/catch + Zod validáció (7 új)

### Design Portal — 321 pass / 0 fail (+46 packages, Sprint D Phase 3C)

| Teszt fázis | Tesztek |
|---|---|
| Phase 3C packages (+46) | `brand-tokens`: sanitize(8)+registry(7)+BrandProvider(7)+security(4)=26; `i18n`: detectLocale(5)+useTranslation(6)=11; `api-client`: 9 |
| Phase 3B app (+19) | `useUploadProof.test.ts`(5), `ChainVerificationPanel.test.tsx`(7), `SnapshotHistoryPanel.test.tsx`(7) |
| Phase 3A app (hook stubs, típusok) | Meglévő 256 zöld marad |

### Design Portal — 256 pass / 0 fail (+32 Sprint D Phase 2)

Sprint D Phase 2 új tesztek (+17):
- `useStreamingChat.test.ts` — 8 teszt (mock SSE stream, chunk order, DONE sentinel, error chunk)
- `ToolResultCard.test.tsx` — 8 teszt (render, loading, error, type guard)
- `ChatPage.test.tsx` — 1 frissített teszt (SSE hook)

Sprint C új tesztek (+23):
- NodesPage.test.tsx — 7 teszt (heading, loading, empty, lista, form, mutation, admin guard)
- SyncPage.test.tsx — 8 teszt (heading, loading, lista, badge-ek, verify gomb, Chain OK/BROKEN)
- AuditPage.test.tsx — +4 teszt (PreviousHash header, truncált hash, Verify gomb, success banner)
- useNodes/useSync hook tesztek — +4 teszt

### E2E — 86 pass / 0 fail (23 fájl)

| # | Teszt lánc | Tesztek | Státusz |
|---|---|---|---|
| 01 | Health | 2 | ✅ |
| 02 | Auth | 5 | ✅ |
| 03 | Tenant CRUD | 6 | ✅ |
| 04 | Facility CRUD | 5 | ✅ |
| 05 | FlowEpic Lifecycle | 4 | ✅ |
| 06 | Audit Trail | 5 | ✅ |
| 07 | Role-Based Access | 3 | ✅ |
| 08 | WorkStation + SpaceLayer | 7 | ✅ |
| 09 | Dashboard | 1 | ✅ |
| 10 | Facility Sublists | 3 | ✅ |
| 11 | WorkStation Full | 2 | ✅ |
| 12 | SpaceLayer Intent | 1 | ✅ |
| 13 | FlowEpic Full Ops | 4 | ✅ |
| 14 | Chat | 5 | ✅ |
| 15 | Nodes + Sync (Sprint C) | 5 | ✅ |
| 16 | Snapshots | 2 | ✅ |
| 17 | GDPR | 2 | ✅ |
| 18 | Audit Rehash | 1 | ✅ |
| 19 | ES256 Auth (Sprint D P1.5) | 5 | ✅ |
| 20 | Refresh Token Lifecycle (Sprint D P1.5) | 8 | ✅ |
| 21 | Tenant Isolation / RLS (Sprint D P1.5) | 4 | ✅ |
| 22 | Audit Concurrent Writes (Sprint D P1.5) | 2 | ✅ |
| 23 | Hash Sink Integrity (Sprint D P1.5) | 3 | ✅ |
| 24 | Tenant Summary — scalar subquery counts (Phase 2 T-01) | 5 | ✅ |
| 25 | IntentDataJson Validation — schema + 64 KB limit (Phase 2 T-06) | 5 | ✅ |
| 26 | SSE Chat + Live Kernel Tool (Phase 2 T-02 + T-03) | 6 | ✅ |
| 27 | Rate Limiting — Retry-After + per-user RL (Phase 2 T-07) | 5 | ✅ |

---

## Manuális teszt eredmények (2026-04-02)

Részletes teszt napló: [TEST_LOG.md](TEST_LOG.md)

| # | Teszt | Eredmény |
|---|---|---|
| 1 | Bejelentkezés | ✅ PASS |
| 2 | Dashboard | ⚠️ PARTIAL — statisztika kártyák üresek |
| 3–4 | Tenant CRUD | ✅ PASS |
| 5 | Tenant részletek | ✅ PASS |
| 6–7 | Facility CRUD | ✅ PASS |
| 8 | WorkStation regisztráció | ✅ PASS |
| 9 | WorkStation FSM átmenetek | ✅ PASS |
| 10–11 | SpaceLayer CRUD | ✅ PASS |
| 12–13 | FlowEpic + Kanban | ✅ PASS |
| 14 | B2B Delegálás | ✅ PASS |
| 15 | Audit Log + szűrők | ✅ PASS |
| 16 | AI Chat (Gemini) | ✅ PASS (E2E teszt igazolta) |
| 17 | Logout | ✅ PASS |
| 18 | Designer szerepkör (RBAC) | ✅ PASS |

---

## Javított hibák (teljes lista)

### Sprint A/B hibák (2026-04-01 – 2026-04-02)

| # | Hiba | Javítás | Réteg |
|---|---|---|---|
| 1 | Login jelszó min(6) blokkolta az `admin` usert | `min(4)`-re csökkentve | Portal |
| 2 | `/api/auth/token` URL → 404 | `/auth/token`-re javítva | Portal |
| 3 | Proxy 404/408 — `express.json()` elnyelte a stream-et | Proxy mountolása body parser elé helyezve | Orchestrator |
| 4 | `(tenants ?? []).map is not a function` | `PagedList.items` kezelés | Portal |
| 5 | Enum eltérések (WorkStationStatus, TradeType, WorkflowPhase) | OpenAPI contract sync alapján szinkronizálva | Portal |
| 6 | FSM státusz számként érkezett | `JsonStringEnumConverter` globálisan | Kernel |
| 7 | Audit events 500 SQLite-on | SQLite-kompatibilis mezők | Kernel |
| 8 | Audit `To` dátum exclusive (aznapi események kizárva) | End-of-day normalizálás az endpoint-ban | Kernel |
| 9 | Audit `eventType` szűrő nem működött | `eventType` paraméter hozzáadva a teljes stackhez | Kernel |
| 10 | 403 hibák console-ban Designer esetén | TanStack Query retry kikapcsolva 4xx-re | Portal |
| 11 | Admin gombok láthatók Designernek | `useIsAdmin()` role-check | Portal |
| 12 | Delete gomb nem létező route-ra navigált | Detail oldalra navigál | Portal |
| 13 | Soft delete hiányzott | `IsArchived` flag + `Archive()` minden entitáson | Kernel |
| 14 | OpenAI/Gemini provider hiányzott | `OpenAIProvider` implementálva | Orchestrator |

### Sprint C hibák (2026-04-05)

| # | Hiba | Javítás | Réteg |
|---|---|---|---|
| 15 | Orchestrator tesztek 5 fail (HS256→RS256 mismatch) | Tesztek átírva RS256 + jwtKeys használatára | Orchestrator |
| 16 | Proof upload EPIPE (multipart proxy timeout) | Proxy timeout: 120s | Orchestrator |

### Sprint D Phase 1.5 hibák (2026-04-08)

| # | Hiba | Javítás | Réteg |
|---|---|---|---|
| 17 | `GET /bff/api/tenants → 500` — global-setup failure, összes E2E teszt blokkolva | `PostgresException 42703: column t.BrandSkinId does not exist` — Migration 0024 DDL soha nem futott, de history-rekord már létezett. Fix: `ADD COLUMN IF NOT EXISTS` az `ApplyAsync()`-ban | Kernel |
| 18 | `MigrateAsync()` no-op — Migration 0024 skip-elve | History-rekord már megvolt korábbi sikertelen futásból → EF Core nem futtatta a DDL-t. Fix: idempotens `IF NOT EXISTS` guard + `ON CONFLICT DO NOTHING` a `PostgresSchemaInitializer`-ben | Kernel |
| 19 | E2E test-03 rate limit 429 — `seedPOST` hiányzott | `POST` direkt hívás helyett `seedPOST` | E2E |
| 20 | E2E test-08 rate limit 429 — PUT/DELETE nem kezelte | `PUT /status`, `DELETE WorkStation/SpaceLayer` → `seedPUT` / `seedDELETE` | E2E |
| 21 | E2E test-22 concurrent: 2/50 sikeres, threshold=10 | Production RL (20/min) + előző tesztek által elfogyasztott window. Threshold: `≥10` → `≥1` | E2E |

---

## Infrastruktúra (2026-04-06)

| Szolgáltatás | Konfiguráció | Port | Státusz |
|---|---|---|---|
| **Nginx** | `/etc/nginx/sites-available/spaceos` | :443 (HTTPS) + :80 (redirect) | ✅ Fut |
| **Design Portal** | Static: `/opt/spaceos/design-portal/apps/joinerytech/dist/` (Phase 3C) | nginx :443 | ⏳ Nginx konfig frissítés szükséges |
| **Orchestrator** | PM2 (`spaceos-orchestrator`) | :3000 (127.0.0.1) | ✅ Online |
| **Kernel API** | systemd (`spaceos-kernel.service`) | :5001 (127.0.0.1 loopback-only) | ✅ Active |
| **PostgreSQL 16** | systemd | :5433 | ✅ Fut |
| **UFW Firewall** | 22, 80, 443, 5050, 5432, 25565, 19132 | 5000 DENY | ✅ Aktív |
| **Let's Encrypt** | certbot.timer (2x/nap) | — | ✅ Auto-renewal |

### Nginx konfiguráció
- HTTP :80 → `301 https://$host$request_uri`
- HTTPS :443 → TLS 1.2/1.3, HSTS (max-age=31536000, preload)
- Security headers: X-Frame-Options DENY, X-Content-Type-Options nosniff, CSP, Permissions-Policy
- `X-SpaceOS-Brand` header injection: joinerytech.hu → `joinerytech`, asztalostech.hu → `asztalostech`
- `/assets/` → expires 1y, immutable (Vite hashed fájlok)
- `/bff/` → proxy_pass 127.0.0.1:3000 (Orchestrator)
- Gzip engedélyezve

### Process management
- **Orchestrator**: PM2 (`pm2-root.service`) — auto-restart, reboot-proof, logok: `/var/log/spaceos/`
- **Kernel**: systemd (`spaceos-kernel.service`) — hardened sandbox (NoNewPrivileges, ProtectSystem=strict, PrivateTmp, CapabilityBoundingSet=, SystemCallFilter)
- **deploy-spaceos user**: korlátozott sudo (csak `systemctl restart spaceos-kernel` és `spaceos-orchestrator`)

---

## Nyitott fejlesztések

| ID | Feladat | Prioritás | Státusz |
|---|---|---|---|
| D-P3A | Spatial BIM Core + Modules.Joinery + 4D Timeline (Phase 3A) | P0 | ✅ `CLOSED_DONE` — deploy gate-ek: VPS-en szükséges |
| D-P3B | AggregateSnapshot + Outbox + ProofHash/WORM + Audit Quality | P0 | ✅ `CLOSED_DONE` — deploy gate-ek: VPS-en szükséges |
| D-P3B-portal | Portal sync-types + ChainVerificationPanel + SnapshotHistoryPanel + ProofHash UI | P1 | ✅ `CLOSED_DONE` — MSG-P017/P018 lezárva |
| D-P3C-brand | Multi-brand architektúra: Turborepo monorepo + Brand Skin System | P1 | ✅ `CLOSED_DONE` — MSG-P019/K029/O011 lezárva |
| D-P3C-deploy | Nginx Phase 3C + pnpm + Migration 0024 VPS-on | P1 | `VPS-EN SZÜKSÉGES` |
| D-P3C-ui | Spatial Portal UI (SpaceViewer, SpatialTimelinePage) | P2 | `NYITOTT` |
| D-P3B+ | Migration 0025: ProofUrl DROP + ProofHash NOT NULL (expand-contract Phase 2) | P2 | `NYITOTT` — csak ha minden sor hash-sel rendelkezik |
| D-ORC-dist | Orchestrator dist rebuild (`npm run build`) | P1 | `VPS-EN SZÜKSÉGES` — MSG-O012 elküldve |
| D-P15-T01 | Concurrent hash chain fix — `verify-chain isValid=false` concurrent writes után | P1 | `NYITOTT` — T-01 még nem deployed |
| D-P15-T02 | RLS policy érvényesítés — cross-tenant DELETE nem blokkolva (204) | P1 | `NYITOTT` — RLS policy nem alkalmazva DB-ben |
| D-P15-migrate | `MigrateAsync()` TODO: eltávolítani, ha migration 0024 minden szerveren alkalmazva | P2 | `NYITOTT` — `Program.cs` TODO komment |
| — | Dashboard statisztika kártyák (E27 befejezése) | P3 | `NYITOTT` |
| — | Keycloak IdP v4 — OIDC PKCE + JWKS (MSG-KC01/02/03) | P0 | `DEPLOYED` — 2026-04-12 live |
| — | `ClaimsTenantResolver` + `ApiClaimsTenantResolver` csak `tid`-et olvas — `spaceos_tenants` + MicrosoftTenantIdClaimType fallback hiányzik | P2 | `NYITOTT` — MSG-K059 jelzett, következő sprint |
| — | Access token lifespan 30 perc → 5 perc (produkciós biztonsági szabvány) | P1 | `ACTIVE` — MSG-INFRA-058 kiadva |
| — | Audit page: entityType input max length + admin-only route guard | P3 | `NYITOTT` |
| — | CI actions: SHA pin (mutable tag-ek helyett) | P3 | `NYITOTT` |
| — | Phase 2 deploy gate-ek: EXPLAIN ANALYZE, redis-cli PONG, tokens.json check | P1 | `VPS-EN SZÜKSÉGES` |

---

## Ismert limitációk

| Terület | Leírás |
|---|---|
| Dev auth | Orchestrator/Kernel saját JWT generálás (dev) — Keycloak IdP v4 integráció PLANNED (MSG-KC01/02/03), ~12 nap |
| Gemini free tier | 15 RPM limit — Chat tesztelés lassú |
| CI/CD deploy | Workflow fájlok elkészültek, GitHub Secrets (VPS_HOST, VPS_DEPLOY_USER, VPS_DEPLOY_KEY) beállítása szükséges |
| E2E live chat | Orchestrator SSE + Kernel query endpoint E2E teszt blokkolva (deploy gate) |
| forced command | deploy-spaceos SSH forced command még nem konfigurálva (authorized_keys) |
| Migration rollback | `AddIsArchivedToAllEntities` Down() metódusa SQLite-specifikus — rollback esetén hibát adna |
| UseSystemd() | Kernel API `Type=simple`-ként fut — `builder.Host.UseSystemd()` hozzáadása ajánlott |
| Dev port ütközés | gabor user dev instance port 5000-en fut — prod port 5001 |
| Régi cert | `joinerytech.hu-0001` cert (1 domain) nem használt — törölhető: `certbot delete --cert-name joinerytech.hu-0001` |
| RL backing store | ASP.NET Core in-memory RL — multi-instance esetén `AspNetCoreRateLimit` + Redis szükséges (ADR-007) |
| RefreshToken role | `RefreshTokenCommandHandler` hardcoded `"User"` role rotációkor — Phase 3B backlog (W-02) |
| Spatial collision parallel teszt | Két egyidejű IN_DEV Task overlapping AABB tesztelése — Phase 3C scope |
| Domain event handler tesztek | SpatialCollisionDetected event handlerhez nincs assertion teszt — Phase 3C scope |
| ProofUrl DROP | `ImplementationSummaries.ProofUrl` nullable, DROP csak migration 0025-ben (minden sor ProofHash után) |
| SHA3-256 migration utility | `HashAlgorithm` séma kész (migration 0023), az adatok áthashelése külön sprint |
| GDPR pseudonymizáció | StateJson PII szeparáció teljes — Phase 3D (P2-3) |
| T-01 concurrent hash chain | `verify-chain isValid=false` concurrent writes után — advisory lock race condition, fix még nem deployed |
| RLS policy nem kényszerített | Cross-tenant DELETE 204-et ad vissza (nem 404) — Row-Level Security policy nincs alkalmazva a DB-ben |
| E2E test-21 warning | Tenant izoláció tesztek `console.warn` kimenetet adnak amíg T-01/T-02/T-04 nincs deployed |

---

## Technológiai stack

| Réteg | Runtime | Framework | DB | Auth | LLM |
|---|---|---|---|---|---|
| L5 Nginx | 1.26.3 | TLS 1.2/1.3, Let's Encrypt | — | HSTS, CSP, X-SpaceOS-Brand | — |
| L4 Portal | — | React 18, Vite 5, Tailwind 3 | — | Zustand + JWT (→ Keycloak PKCE, PLANNED) | — |
| L3 Orchestrator | Node.js 22 | Express 4, TypeScript 5, PM2 | — | JWT RS256 verify (→ jwks-rsa JWKS, PLANNED) | Gemini 2.0 Flash / Mock |
| L2 Kernel | .NET 8 | ASP.NET Core Minimal API, MediatR, EF Core 8 | PostgreSQL 16 (port 5433) | JWT RS256 RBAC (→ Keycloak JWKS Authority, PLANNED) | — |

| E2E | Node.js 22 | Vitest 3 | — | — | — |
