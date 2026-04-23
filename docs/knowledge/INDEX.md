# SpaceOS Knowledge Base — INDEX

> Minden knowledge doc 2-3 soros összefoglalója. Hidegindításhoz olvasd el ezt először.
> Létrehozva: 2026-04-17 · Frissítve: 2026-04-20 (LIB-002) · Forrás: ~200+ mailbox üzenet feldolgozva

---

## SECURITY

### [`security/SECURITY_PATTERNS.md`](security/SECURITY_PATTERNS.md)
Konkrét kódminták minden biztonsági pattern-hez: `MapInboundClaims=false`, `ValidateAudience=true`, `DenyWebRequestSentinel`, RLS policy SQL, `TenantSessionInterceptor` teljes implementáció, SSRF allowlist regex, pageSize clamp, rate limit, internal service guard.

### [`security/SECURITY_DECISIONS.md`](security/SECURITY_DECISIONS.md)
Minden biztonsági döntés, elfogadott kockázat és indoklás táblázatos formában. Sprint 6 security review összesítő: 0 kritikus nyitott, javított M01/M02/M03/K1/K2 findingek. Vite CVE kockázatelfogadás, kernel.env 644 kockázatelfogadás.

---

## DEPLOYMENT

### [`deployment/DEPLOYMENT_RUNBOOK.md`](deployment/DEPLOYMENT_RUNBOOK.md)
Step-by-step deploy guide minden service-hez. Port térkép (Kernel 5000, Orch 3000, Joinery 5002...), systemd service nevek, PM2, env fájlok teljes tartalma, Keycloak bare-metal setup, Admin CLI parancsok, Nginx konfig, rollback terv.

### [`deployment/KNOWN_GOTCHAS.md`](deployment/KNOWN_GOTCHAS.md)
18 konkrét csapda amit átéltünk: `rm -rf publish` kötelezősége, migration bypass, Keycloak hostname fix, `spaceos_tenants` double-serialization, `MapInboundClaims` bug, GUC regisztráció, rate limit E2E-ban, Portal CSP fix, Docker→bare-metal Keycloak. **Új (LIB-002):** `DateTime.SpecifyKind` Npgsql timestamptz, `EnableRetryOnFailure` + explicit transaction conflict, MinIO DI üres credentials exception.

---

## FEJLESZTÉSI MINTÁK

### [`patterns/DEV_DIFFICULTIES.md`](patterns/DEV_DIFFICULTIES.md)
Visszatérő problémák timeline-ja: `ClaimsTenantResolver` 4 iterációs saga, migration reconcile procedúra, GUC key naming mismatch (app.tenant_id vs app.current_tenant_id modulonként), InventoryProviderStub→HttpAdapter váltás, nesting aktiválás 6 lépéses debug lánca, ASPNETCORE_ENVIRONMENT hiánya.

### [`patterns/DATABASE_PATTERNS.md`](patterns/DATABASE_PATTERNS.md)
RLS policy SQL, `DbConnectionInterceptor` teljes implementáció GUC beállítással, migration naming konvenció, Testcontainers integráció, DEFAULT PRIVILEGES grant, WORM hash sink, advisory lock MD5 key, `IDbContextFactory`, `try_cast_uuid` PostgreSQL függvény, EF query filter DenyWebRequestSentinel logikával.

### [`patterns/TESTING_PATTERNS.md`](patterns/TESTING_PATTERNS.md)
E2E teszt fájl struktúra (Vitest+fetch), global-setup.ts szerepe, probe-and-skip minta, 401/200 auth teszt kötelezősége minden endpointon, rate limit exhaustion kezelése (85s wait), cross-tenant isolation teszt minta, helpers API definíció, `fileParallelism: false` kötelezősége.

---

## ARCHITEKTÚRA

### [`architecture/ADR_CATALOGUE.md`](architecture/ADR_CATALOGUE.md)
17 ADR (ADR-005..ADR-037): advisory lock, SpatialContractDto, StageHandoff Infrastructure réteg, ES256→Keycloak, ProviderStub/HttpAdapter, DbConnectionInterceptor, DenyWebRequestSentinel, double serialization, connection affinity, GenesisHash. **Új (LIB-002):** ADR-031..037 — SourceChannel Shared namespace, DIM SubmitAnonymousSheetAsync, CuttingAnonymous flag pozíció, AnonymousSheetRequest wrapper, Nesting NuGet önállóság, CuttingPlanStatus enum migration, EnableRetryOnFailure eltávolítása.

### [`architecture/API_CONTRACT_CATALOGUE.md`](architecture/API_CONTRACT_CATALOGUE.md)
Teljes endpoint lista minden service-hez: Kernel (tenant, facility, flowepic, stage, spatial, nodes, audit, health), Orchestrator BFF (/bff/*), Joinery, Abstractions, Inventory, Cutting, Procurement. Auth követelmény minden endpointnál jelezve.

### [`architecture/MODULE_BOUNDARIES.md`](architecture/MODULE_BOUNDARIES.md)
Provider interfészek, Contracts NuGet verziók (**1.3.0** — SourceChannel, AnonymousSheetRequest, CuttingAnonymous DIM), **SpaceOS.Nesting.Algorithms NuGet** (FFDH+Guillotine), **ICuttingEventPublisher HTTP event bus** (Cutting→Inventory), Orchestrator route sorrend, DB szeparáció, RBAC policy-k.

---

## TERMINÁL KONTEXTUSOK

### [`context/KERNEL_CONTEXT.md`](context/KERNEL_CONTEXT.md)
Kernel (.NET 8, port 5000). **1138 pass** · commit 46d64b5 LIVE · 🚀 Soft Launch 2026-04-20. Kritikus: `MapInboundClaims=false`, `DenyWebRequestSentinel`, `EnableRetryOnFailure` eltávolítva (ADR-037), MinIO conditional DI (KERNEL-089).

### [`context/ORCH_CONTEXT.md`](context/ORCH_CONTEXT.md)
Orchestrator (Node.js 22, PM2, port 3000). **219 pass** · commit 7b16acb. Chat 422 fix: Zod discriminated union üres assistant content-re. SSE endpoint: `/bff/chat/stream` (nem `/bff/chat`).

### [`context/PORTAL_CONTEXT.md`](context/PORTAL_CONTEXT.md)
Portal (React 18, Vite, Turborepo). **323 pass** · turbo build OK. Keycloak PKCE flow. BUG-009..016 lezárva. Doorstar Portal külön: `spaceos-doorstar-portal/` 306 pass LIVE.

### [`context/JOINERY_CONTEXT.md`](context/JOINERY_CONTEXT.md)
Joinery (.NET 8, port 5002, QuestPDF). **249 pass** · commit 35a8723. JOINERY-016: hardware-list-pdf + material-req-pdf. ⚠️ publish chown fix szükséges.

### [`context/CUTTING_CONTEXT.md`](context/CUTTING_CONTEXT.md)
Cutting + Inventory + Procurement (.NET 8, portok 5004-5006). **195/154/53 pass**. CuttingPlan FSM (Draft→Published→Frozen→Closed), SpaceOS.Nesting.Algorithms NuGet, ICuttingEventPublisher HTTP event bus. Session B: CUTTING-031 deploy szükséges.

### [`context/INFRA_CONTEXT.md`](context/INFRA_CONTEXT.md)
VPS 109.122.222.198. Systemd service-ek, PM2, Nginx, Keycloak bare-metal (nem Docker!), PostgreSQL 5433. Deploy minta, rollback, Keycloak Admin CLI quick ref. Nyitott: kernel.env 644→640.

### [`context/E2E_CONTEXT.md`](context/E2E_CONTEXT.md)
E2E teszt suite (Vitest+fetch). **266/266 pass · 56 fájl** · ~40s. Soft Launch verified ✅. Probe-skip: Inventory inbound (BUG-003), Reservation API, Brand Skin, Refresh token. `fileParallelism: false` kötelező.

---

## Legfontosabb 5 tanulság (quick start)

1. **`MapInboundClaims = false`** — minden .NET JWT validáló service-ben kötelező, különben `tid` claim MS URI-ra mappelődik és az RLS nem működik
2. **GUC kulcs `app.current_tenant_id`** — minden modulban ugyanez a névkonvenció, `ALTER DATABASE SET` init lépés kötelező
3. **`spaceos_tenants` claim snake_case, double-serialized** — `tenant_id` (nem `tenantId`), outer JSON string wrapper
4. **`rm -rf publish/*` deploy előtt** — inkrementális build nélkül DLL mismatch
5. **`requireAuth` mindig a rate limiter előtt** — különben unauthentikált kérések 429-et kapnak 401 helyett
