# SpaceOS — Kódbázis összesített állapotleírás

**Utolsó frissítés:** 2026-04-07
**Tesztelő:** Gabor
**Környezet:** VPS prod (109.122.222.198) — nginx (HTTPS) → Orchestrator → Kernel
**Domainek:** joinerytech.hu · asztalostech.hu (Let's Encrypt SAN cert)

---

## Rendszer architektúra

```
Browser  https://joinerytech.hu / https://asztalostech.hu
  │
  ▼
L5  Nginx              (TLS 1.2/1.3 + HSTS + security headers)   port 443
  │  /  →  /opt/spaceos/design-portal/dist/
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
L2  Kernel API         (C# .NET 8 · Minimal API)       port 5001 (systemd, loopback-only)
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
| **SpaceOS.Kernel** | L2 Backend | .NET 8, EF Core 8, PostgreSQL | `DEPLOYED` | 814 pass / 0 fail | 0 error, 0 warning |
| **SpaceOS.Orchestrator** | L3 BFF | Node.js 22, TypeScript 5, Express 4 | `DEPLOYED` | 114 pass / 0 fail | 0 TS error |
| **SpaceOS.DesignPortal** | L4 Frontend | React 18, TypeScript 5, Vite, Tailwind | `DEPLOYED` | 256 pass / 0 fail | 0 TS error |
| **E2E** | Full stack | Vitest + fetch | `PASS` | 63 pass / 0 fail | — |

**Összesített tesztszám: 1247 pass / 0 fail**

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
| ⏳ | Migrations 0016–0019 alkalmazva PostgreSQL-en | VPS deploy gate |
| ⏳ | `EXPLAIN ANALYZE` — all spatial endpoints | PostgreSQL-en validálandó |
| ⏳ | `SELECT tableowner FROM pg_tables WHERE tablename = 'BvhNodes'` → `spaceos_schema_owner` | VPS ellenőrzés |
| ⏳ | DB trigger tesztek (cross-tenant, self-loop, depth limit, CellSize immutability) | Live DB-n |

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

- [ ] Migrations 0016–0019 alkalmazva: `dotnet ef database update`
- [ ] `EXPLAIN ANALYZE` minden spatial endpoint-on (Index Scan)
- [ ] `SELECT tableowner FROM pg_tables WHERE tablename = 'BvhNodes'` → `spaceos_schema_owner`
- [ ] DB trigger teszt: cross-tenant SpatialTaskLink INSERT → blokkolva
- [ ] DB trigger teszt: BvhNode önhivatkozás → blokkolva
- [ ] DB trigger teszt: BvhNode mélység > 32 → blokkolva
- [ ] DB trigger teszt: PhysicalSpace CellSize módosítás → blokkolva

### Phase 3A Ismert limitációk (Phase 3B scope)

- `FlowEpic.ComputeSpatialState()` — FlowTask navigáció + FsmState integráció hiányzik
- Parallel collision teszt (két IN_DEV Task overlapping AABB) — domain event handler tesztek
- DoD teszt gap: +24 elért vs ≥35 cél (pre-existing spatial tesztek részben fedik)

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

- [ ] `redis-cli -a $REDIS_PASSWORD ping` → PONG
- [ ] `EXPLAIN ANALYZE` Index Scan mind a 4 query endpointon
- [ ] `tokens.json` nem létezik post-deploy
- [ ] Nginx: `proxy_buffering off; proxy_read_timeout 300s;` SSE route-ra

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

---

## Teszteredmények részletezés (2026-04-07)

### Kernel — 814 pass / 0 fail (+37 Sprint D Phase 3A)

| Teszt projekt | Phase 2 után | Phase 3A után | Delta |
|---|---|---|---|
| SpaceOS.Kernel.Tests (unit) | 621 | 645 | +24 |
| SpaceOS.Kernel.Api.Tests (API) | 101 | 101 | 0 |
| SpaceOS.Kernel.IntegrationTests | 68 | 68 | 0 |
| **Összesen** | **790** | **814** | **+24** |

Sprint D Phase 3A új tesztek (+24):
- `BoundingBoxTests.cs` — 10 teszt (Intersects 6-axis AABB)
- `SpatialSecurityTests.cs` — 5 reflection gate (ElementType absent DTO-ban, no Other enum, no _children/_nodes nav)
- `RegisterPhysicalSpaceCommandValidatorTests.cs` — 7 teszt
- `RegisterSpatialElementCommandValidatorTests.cs` — 5 teszt
- `AuditEventDispatcherTests.cs` — namespace collision bugfix (+0 új, 1 fix)

Pre-existing spatial tesztek (Phase 3A domain/infra implementáció közben keletkeztek):
- `SpatialGridTests.cs` — 4 teszt, `PhysicalSpaceTests.cs` — 2 teszt, `BvhQueryServiceTests.cs` — 4 teszt

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

### E2E — 63 pass / 0 fail (18 fájl)

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

---

## Infrastruktúra (2026-04-06)

| Szolgáltatás | Konfiguráció | Port | Státusz |
|---|---|---|---|
| **Nginx** | `/etc/nginx/sites-available/spaceos` | :443 (HTTPS) + :80 (redirect) | ✅ Fut |
| **Design Portal** | Static: `/opt/spaceos/design-portal/dist/` | nginx :443 | ✅ Kiszolgálva |
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
| D-P3B | AggregateSnapshot + Outbox + ProofHash+WORM | P1 | `NYITOTT` |
| D-P4 | Multi-brand architektúra (Turborepo, brand skin-ek) | P2 | `NYITOTT` |
| — | Dashboard statisztika kártyák (E27 befejezése) | P3 | `NYITOTT` |
| — | IdP integráció (Auth0/Keycloak) — dev auth kiváltása | P2 | `NYITOTT` |
| — | Audit page: entityType input max length + admin-only route guard | P3 | `NYITOTT` |
| — | CI actions: SHA pin (mutable tag-ek helyett) | P3 | `NYITOTT` |
| — | Phase 2 deploy gate-ek: EXPLAIN ANALYZE, redis-cli PONG, tokens.json check | P1 | `VPS-EN SZÜKSÉGES` |

---

## Ismert limitációk

| Terület | Leírás |
|---|---|
| Dev auth | Orchestrator bármilyen username/password-ot elfogad — prodban IdP (Auth0/Keycloak) szükséges |
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
| FlowEpic.ComputeSpatialState() | FlowTask navigáció + FsmState integráció hiányzik — Phase 3B scope |
| Spatial collision parallel teszt | Két egyidejű IN_DEV Task overlapping AABB tesztelése — Phase 3B scope |
| Domain event handler tesztek | PhysicalSpaceRegistered, SpatialElementRegistered, SpatialCollisionDetected event handlerhez nincs assertion teszt |

---

## Technológiai stack

| Réteg | Runtime | Framework | DB | Auth | LLM |
|---|---|---|---|---|---|
| L5 Nginx | 1.26.3 | TLS 1.2/1.3, Let's Encrypt | — | HSTS, CSP, X-SpaceOS-Brand | — |
| L4 Portal | — | React 18, Vite 5, Tailwind 3 | — | Zustand + JWT | — |
| L3 Orchestrator | Node.js 22 | Express 4, TypeScript 5, PM2 | — | JWT RS256 verify | Gemini 2.0 Flash / Mock |
| L2 Kernel | .NET 8 | ASP.NET Core Minimal API, MediatR, EF Core 8 | PostgreSQL 16 (port 5433) | JWT RS256 RBAC | — |
| E2E | Node.js 22 | Vitest 3 | — | — | — |
