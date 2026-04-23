---
id: MSG-LIB-001
from: root
to: librarian
type: task
priority: high
status: READ
created: 2026-04-17
---

# LIB-001 — Teljes mailbox szintézis: tudásbázis felépítése

## Cél

Feldolgozni az összes archivált mailbox üzenetet és strukturált tudásdokumentumokat
létrehozni `docs/knowledge/` alatt, amelyek alapján:
- Jövőbeli fejlesztések gyorsan elindíthatók
- Terminálok hideg indítással is teljes kontextust kapnak
- Biztonsági és architektúra döntések visszakereshetők

## Forrásdokumentumok

```
docs/mailbox/kernel/archive/       (~38 fájl)
docs/mailbox/orchestrator/archive/ (~35 fájl)
docs/mailbox/portal/archive/       (~19 fájl)
docs/mailbox/abstractions/archive/ (~8 fájl)
docs/mailbox/e2e/archive/          (~11 fájl)
docs/mailbox/infra/archive/        (~13 fájl)
docs/mailbox/joinery/archive/      (~4 fájl)
docs/mailbox/*/outbox/             (lezárt DONE üzenetek)
docs/Codebase_Status.md
docs/tasks/archive/
```

## Kötelező kimenetek (prioritás sorrendben)

### 1. Biztonsági dokumentáció
- `docs/knowledge/security/SECURITY_PATTERNS.md`
  - Sprint 6 security review: minden K1/K2/K3/M01-M03/M1-M3 finding + fix
  - OWASP alkalmazások: input validáció, SSRF allowlist, err.message szivárgás
  - JWT / RBAC minták: MapInboundClaims, ValidateAudience, tid claim
  - RLS implementáció: DenyWebRequestSentinel, MapInboundClaims=false

- `docs/knowledge/security/SECURITY_DECISIONS.md`
  - Vite CVE (GHSA-4w7w-66w2-5vf9) — elfogadott kockázat + miért
  - Procurement /healthz hiánya — tech debt + kockázat szint
  - Rate limit konfigurációs döntések

### 2. Deployment dokumentáció
- `docs/knowledge/deployment/DEPLOYMENT_RUNBOOK.md`
  - VPS struktúra: portok, service nevek, systemd units
  - Deploy sorrend (minden modulhoz lépésről lépésre)
  - Keycloak JAR deploy, realm export, script mapper
  - Env fájlok helye: `/etc/spaceos/*.env`
  - Publish path gotcha: `rm -rf publish` inkrementális build előtt

- `docs/knowledge/deployment/KNOWN_GOTCHAS.md`
  - GenesisHash env gotcha (INFRA-096)
  - Migration bypass procedúra
  - pm2 vs systemd különbség a moduloknál
  - Keycloak KC_HOSTNAME_ADMIN fix
  - sudoers tartós fix
  - spaceos_tenants vs tid claim értelmezés
  - GUC regisztráció (ALTER DATABASE app.current_tenant_id)
  - USAGE+DML+SEQUENCES grant szükségessége új DB-nél

### 3. Fejlesztési minták
- `docs/knowledge/patterns/DEV_DIFFICULTIES.md`
  - ClaimsTenantResolver debugolás (4+ iteráció)
  - Migration reconcile procedúra
  - TenantSessionInterceptor GUC key naming (`app.tenant_id` vs `app.current_tenant_id`)
  - OpenConnectionAsync affinity fix (JOINERY-014 minta)
  - Rate limit window exhaustion E2E-ban (60s wait)

- `docs/knowledge/patterns/DATABASE_PATTERNS.md`
  - RLS policy szerkezet (minden modulban)
  - GUC set_config minta (DbConnectionInterceptor)
  - Migration naming konvenció
  - Testcontainers integráció

- `docs/knowledge/patterns/TESTING_PATTERNS.md`
  - E2E teszt struktúra (Vitest + fetch, helpers.ts, global-setup)
  - Testcontainers használat (SEC-01/SEC-02 minta)
  - Probe-and-skip minta nesting shape teszthez

### 4. Architektúra dokumentáció
- `docs/knowledge/architecture/ADR_CATALOGUE.md`
  - ADR-023 és többi ADR kigyűjtve az üzenetekből
  - Minden döntés: kontextus, döntés, következmény

- `docs/knowledge/architecture/API_CONTRACT_CATALOGUE.md`
  - Kernel endpoints (összes)
  - Orchestrator BFF routes (összes)
  - Modul endpoints (Inventory/Cutting/Procurement/Joinery/Abstractions)

- `docs/knowledge/architecture/MODULE_BOUNDARIES.md`
  - IInventoryProvider, ICuttingProvider, IProcurementProvider interfészek
  - Contracts NuGet csomagok (verziók, tartalom)
  - ProviderAdapter vs ProviderHttpAdapter minta

### 5. Terminál kontextusok
Minden fájlhoz: ki ez a terminál, mi a felelőssége, mi az aktuális állapota,
mi a legfontosabb amit tudni kell az induláshoz.

- `docs/knowledge/context/KERNEL_CONTEXT.md`
- `docs/knowledge/context/ORCH_CONTEXT.md`
- `docs/knowledge/context/PORTAL_CONTEXT.md`
- `docs/knowledge/context/JOINERY_CONTEXT.md`
- `docs/knowledge/context/CUTTING_CONTEXT.md`
- `docs/knowledge/context/INFRA_CONTEXT.md`
- `docs/knowledge/context/E2E_CONTEXT.md`

### 6. Index
- `docs/knowledge/INDEX.md` — minden doc 2-3 soros összefoglalója + elérési út

## DONE feltételek

- [ ] Mind a 15+ knowledge doc megvan tartalommal
- [ ] Minden állítás mögött forrásreferencia (`[MSG-XXX-NNN]`)
- [ ] INDEX.md tartalmaz minden doc-ot
- [ ] Outbox DONE üzenet Root-nak

## Skill

Használd a `/spaceos-librarian` skillt session elején.
Sub-agent **engedélyezett** a párhuzamos feldolgozáshoz.
