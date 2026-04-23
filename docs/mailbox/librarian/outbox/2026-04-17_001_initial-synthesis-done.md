---
id: MSG-LIB-001-DONE
from: librarian
to: root
type: done
priority: normal
status: READ
ref: MSG-LIB-001
created: 2026-04-17
---

# Librarian Initial Synthesis — DONE

## Elkészült fájlok

### SECURITY (2 fájl)
- `docs/knowledge/security/SECURITY_PATTERNS.md` — JWT/RBAC minták, RLS SQL, TenantSessionInterceptor implementáció, SSRF allowlist regex, Axios CVE, Vite CVE elfogadott kockázat
- `docs/knowledge/security/SECURITY_DECISIONS.md` — Sprint 6 security review összesítő, 10 ADR-SEC, elfogadott kockázatok indoklásával

### DEPLOYMENT (2 fájl)
- `docs/knowledge/deployment/DEPLOYMENT_RUNBOOK.md` — Port térkép, systemd nevek, env fájlok teljes tartalma, Keycloak bare-metal setup, Admin CLI, Nginx, rollback
- `docs/knowledge/deployment/KNOWN_GOTCHAS.md` — 15 konkrét csapda amit átéltünk (rm -rf publish, migration bypass, MapInboundClaims, GUC regisztráció, rate limit E2E, stb.)

### FEJLESZTÉSI MINTÁK (3 fájl)
- `docs/knowledge/patterns/DEV_DIFFICULTIES.md` — ClaimsTenantResolver 4 iterációs saga, migration reconcile, nesting debug lánc, ASPNETCORE_ENVIRONMENT hiánya
- `docs/knowledge/patterns/DATABASE_PATTERNS.md` — RLS SQL, DbConnectionInterceptor, migration naming, Testcontainers, WORM, advisory lock MD5
- `docs/knowledge/patterns/TESTING_PATTERNS.md` — E2E struktúra, global-setup, probe-and-skip, 401/200 minta, rate limit kezelés

### ARCHITEKTÚRA (3 fájl)
- `docs/knowledge/architecture/ADR_CATALOGUE.md` — 10 ADR az üzenetekből
- `docs/knowledge/architecture/API_CONTRACT_CATALOGUE.md` — Minden endpoint (Kernel + Orch BFF + 5 modul)
- `docs/knowledge/architecture/MODULE_BOUNDARIES.md` — IInventoryProvider/ICuttingProvider/IProcurementProvider, Contracts NuGet, ProviderStub vs HttpAdapter, DB szeparáció

### TERMINÁL KONTEXTUSOK (7 fájl)
- `docs/knowledge/context/KERNEL_CONTEXT.md`
- `docs/knowledge/context/ORCH_CONTEXT.md`
- `docs/knowledge/context/PORTAL_CONTEXT.md`
- `docs/knowledge/context/JOINERY_CONTEXT.md`
- `docs/knowledge/context/CUTTING_CONTEXT.md`
- `docs/knowledge/context/INFRA_CONTEXT.md`
- `docs/knowledge/context/E2E_CONTEXT.md`

### INDEX (1 fájl)
- `docs/knowledge/INDEX.md` — Minden doc 2-3 soros összefoglalója + legfontosabb 5 tanulság

**Összesen: 18 fájl létrehozva**

---

## Feldolgozott forrásüzenetek

- Kernel archive: ~38 fájl
- Orchestrator archive: ~35 fájl
- Portal archive: ~19 fájl
- Abstractions archive: ~12 fájl (archive + outbox)
- E2E archive + outbox: ~45+ fájl
- Infra archive: ~13 fájl
- Joinery archive: ~4 fájl
- Cutting outbox: ~15 fájl
- Codebase_Status.md

**Becsült forrásüzenet szám: ~130-140 fájl**

---

## Legfontosabb 5 tanulság

1. **`MapInboundClaims = false` kritikus** — ASP.NET Core default átnevezi a `tid` claim-et MS URI-ra → minden tenant-scoped kérés csendesen üres eredményt ad. Minden .NET service-ben kötelező, és éles regressziót okozott (KERNEL-082).

2. **GUC kulcs egységesítés (`app.current_tenant_id`)** — Modulonként különböző GUC kulcs (app.tenant_id vs app.current_tenant_id) az RLS bypass egyik fő forrása volt. Az `ALTER DATABASE SET` init lépés kötelező minden új DB-nél.

3. **Migration bypass procedúra szükséges** — Az EF Core nem mindig megbízható production migration-öknél (SQLite ModelSnapshot alapú regenerálás PostgreSQL DB-n). Raw SQL bypass + kézi EF history INSERT bevett gyakorlat.

4. **`spaceos_tenants` claim double-serialized snake_case** — A Keycloak Script Mapper kimenet nem natív JSON array, hanem JSON string. ASP.NET Core szét is bontja claim-enként. A `tenant_id` (snake_case, nem camelCase) kulcs keresése kötelező.

5. **requireAuth > rateLimiter sorrend az Orchestratorban** — Ha fordított a sorrend, unauthentikált kérések rate limiter ablakból kapnak 429-et 401 helyett. Ez nehezen diagnosztizálható E2E hibát okoz.
