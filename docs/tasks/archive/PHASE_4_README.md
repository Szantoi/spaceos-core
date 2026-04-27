# Phase 4: Cutting Execution — Claude Code agent context

> Ez a `PHASE_4_README.md` a Claude Code agent-ek belépési pontja a Cutting Phase 4 implementációjához.
> Az autoritatív tervdokumentum: `SpaceOS_Modules_Cutting_Phase4_Execution_Architecture_v4.md`.

---

## 1. Mit kell elérni

A `CuttingSheet` Phase 3-ban már intake-elhető és nesting-publikálható. Phase 4 hozza el a **végrehajtás teljes életciklusát**:

- **Schedule** → execution gép-géphez és időponthoz rendelése
- **Start** → operátor badge tap + worker assignment
- **RecordProgress** → panel-szintű, idempotens, **HMAC-aláírt** progress event-ek
- **RecordOffcut** → vágás közben keletkező hulladékok, **atomic** Cutting+Inventory INSERT
- **Complete** → 3-szintű proof (Hash / Signed / Photo) + crypto-grade evidence
- **Cancel** → bárhol, audit-trail-megőrző állapotátmenet (fizikai DELETE tilos)
- **Milestone Registry** → cross-domain mérföldkő-feliratkozás (Manufacturing, Logistics, Installation Phase 5+ használja)
- **Cross-tenant Handshake** → Doorstar→LapMester pull-only progress + ETag rate-limit
- **Worker consent management** → enrollment + async withdrawal + retroactive photo re-blur

---

## 2. Architektúra-axiómák — 21 db, mind kötelező

| ID | Axióma | Miért kritikus |
|----|--------|----------------|
| A4-1 | `CuttingSheet` immutable | D-06 öröklés Phase 3-ból |
| A4-2 | Sheet → 0..1 aktív Execution | Cancel után új execution indítható |
| A4-3 | Két különálló FSM | Sheet ≠ Execution |
| A4-4 | Mérföldkő rugalmas, befejezés szigorú | Stage Registry alapelve |
| A4-5 | In-tenant real-time, cross-tenant pull-only | DOS-prevention |
| A4-6 | Append-only progress events | UPDATE+DELETE DB-trigger tilos |
| A4-7 | Idempotens progress POST | UUID v7 EventId |
| A4-8 | Completion proof minimum kötelező | Level 0 mindig |
| A4-9 | Crypto-shredding GDPR Art. 17 | Kulcs-törlés = effektív erasure |
| A4-10 | Cross-tenant policy `max(issuer, executor)` | Schedule fázisban guard |
| A4-11 | Hash-chain integration | Per-tenant chain (P1-8 LIVE) |
| A4-12 | Approved package list érintetlen | Sidecar Python ML, nem Kernel-dep |
| A4-13 | Atomicity over eventual consistency | Cutting+Inventory egy commit |
| A4-14 | Secrets schema-isolation | Külön DB schema + role |
| A4-15 | Aggregate fizikai DELETE tilos | DB-trigger BLOCK |
| A4-16 | Két-slot KEK lifecycle | PRIMARY+PREVIOUS koegzisztál |
| A4-17 | Per-event worker HMAC | Anti badge-spoofing |
| A4-18 | Async consent withdrawal | HTTP 202 + status URL |
| A4-19 | In-process adapter only | AssemblyLoadContext startup-assert |
| A4-20 | Outbox-pattern domain event delivery | Kernel outbox re-use |
| A4-21 | Per-batch DbContext scope BackgroundService-ben | Memory + connection pool starvation prevention |

---

## 3. Modulszerkezet (új projektek)

```
SpaceOS.Modules.Cutting.Execution
├── Domain/                      ← 0 external dep, pure C#
├── Application/                 ← MediatR + FluentValidation + Ardalis.Result
├── Infrastructure/              ← EF Core 8 + Npgsql + SignalR + Redis cache
└── Api/                         ← ASP.NET Core 8 Minimal API + JWT + SignalR Hub

SpaceOS.Modules.Cutting.Execution.Contracts
├── Public DTOs
└── Cross-tenant Handshake schema

SpaceOS.Modules.Workers.Consent
└── Worker enrollment + consent withdrawal queue + retroactive processor

SpaceOS.Sidecar.ImageHardening      (Python 3.11, NEM .NET csomag)
├── FastAPI app
├── insightface (ONNX) face detection
├── Pillow (process-isolated worker)
└── python-magic MIME validation
```

---

## 4. Approved package list (mit lehet `<PackageReference>`-szel hozzáadni)

| Csomag | Hol használjuk |
|--------|----------------|
| `MediatR` | Application: command/query bus |
| `FluentValidation` | Application: shape validation (BE-A07) |
| `Ardalis.Result` | Domain + Application: Result<T> |
| `Ardalis.Specification` | Infrastructure: minden list query (Golden Rule 5) |
| `Microsoft.EntityFrameworkCore` (8.x) | Infrastructure |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Infrastructure |
| `xunit.v3` | Tests |
| `Moq` | Tests |
| `Microsoft.AspNetCore.SignalR` | Api: in-tenant real-time |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | Infrastructure: cross-tenant ETag cache |
| `Standart.Hash.xxHash` | Domain: deterministic 63-bit advisory lock (SEC-04) |
| `Serilog.Sinks.File` | Infrastructure: structured audit log (SEC-13) |
| `Microsoft.Extensions.Http` | Api/Infrastructure: IHttpClientFactory (BE-A04) |

**Bármi más → discuss before adding (Master Prompt rule).**

---

## 5. 3 új migration

| Migration | Schema | Cél |
|-----------|--------|-----|
| **C-0004** | `spaceos_cutting` | 4 tábla (CuttingExecutions, CuttingProgressEvents, CuttingOffcutReports, CuttingMilestoneSubscriptions) + RLS WITH CHECK + DB-triggers (append-only, prevent-delete) + 8 index + 4 CHECK constraint |
| **C-0005** | `spaceos_cutting_secrets` | Külön schema + `spaceos_keyvault_role` NOINHERIT NOLOGIN; `CuttingExecutionKeys` tábla két-slot KEK metaadattal (`KekVersion`, `ErasedAt`) |
| **C-0006** | `spaceos_cutting` | `ConsentWithdrawalRequests` tábla async withdrawal queue-hoz + status-tracking |

**Mind idempotens** (`CREATE TABLE IF NOT EXISTS`, `DROP TRIGGER IF EXISTS … CREATE TRIGGER`, `CREATE OR REPLACE FUNCTION`).

---

## 6. Kötelező Specifikációk (9 db, BE-A03 alapján)

```
Domain/Specifications/
├── CuttingExecutionByIdSpec.cs              (single lookup)
├── ActiveExecutionsByTenantSpec.cs          (foreman dashboard)
├── ExecutionsBySheetSpec.cs                 (sheet → execution history)
├── ExecutionsByMachineAndDateSpec.cs        (daily plan view)
├── ExecutionsByHandshakeEpicSpec.cs         (cross-tenant pull)
├── PendingMilestonesSpec.cs                 (Manufacturing/Logistics module subscribers)
├── ExecutionsByConsentScopeSpec.cs          ✨ új — consent withdrawal flow (BE-A03)
├── ConsentAffectedPhotoCountSpec.cs         ✨ új — withdrawal scope counter (BE-A03)
└── ExecutionKeyByExecutionSpec.cs           ✨ új — secrets schema lookup (BE-A03)
```

**Repository-ban raw SQL TILOS** (Golden Rule 5, `grep -r "FromSqlRaw\|ExecuteSqlRaw" Repositories/` → 0 találat, kivéve advisory lock acquisition).

---

## 7. Security gates — DEPLOYMENT BLOCKERS (12 csoport)

| Gate kategória | Mit ellenőriz |
|----------------|---------------|
| GDPR-1..6 | Crypto-shredding, EXIF strip, face blur default, consent record, retention, **withdrawal flow async** |
| HASH-1..2 | Hash-chain commit + verifier passes (BatchId+SeqNum reproducibility) |
| KEK-1 | Két-slot rotation tesztelve, PRIMARY+PREVIOUS koegzisztencia |
| ZERO-1 | AES kulcs `CryptographicOperations.ZeroMemory` |
| AUTH-1..2 | Cross-tenant epic-tenant pair binding + sidecar mTLS+SPKI pin |
| JWT-1 | `ValidAlgorithms = ["RS256"]` allowlist |
| BADGE-1 | Per-event HMAC validation (FixedTimeEquals) |
| MIME-1 | libmagic + bytes-level header validation |
| BATCH-1 | Hash-chain event-batch ordering reproducible |
| REPLAY-1..3 | 5min drift cap + UUID idempotency + sidecar TTL 30s |
| DOS-1..3 | Rate-limit + advisory lock concurrency + sidecar process-isolation |
| DATA-1..3 | Secrets schema isolation + DELETE-block + in-process adapter |

**+ Új BE-gate (9 db):**
- OUT-1/2 (outbox-tx atomicity + idempotens dispatch)
- SCOPE-1/2 (BackgroundService scope per batch)
- REPO-1 (no raw SQL)
- HTTP-1 (IHttpClientFactory handler reuse)
- CONN-1/2 (Cutting+Inventory connection-string egyenlőség)
- QUERY-1 (AsSplitQuery → no cartesian product)

---

## 8. Test gates

| Test típus | Minimum darab |
|-----------|---------------|
| Unit (Domain + Application) | ≥ 95 |
| Integration (Infra + DB + EF + RLS + outbox + concurrent + crypto) | ≥ 60 |
| API (commands + queries + cross-tenant + rate-limit + 3 OpenAPI snapshot) | ≥ 30 |
| SignalR (broadcast + group auth + outbox→fan-out) | ≥ 10 |
| Sidecar Python (libmagic + cgroup + HMAC body-sig) | ≥ 15 |
| **Phase 4 új tesztek összesen** | **≥ 210** |
| Phase 3 Cutting tests (303) | zöld kell maradjon |
| Platform tests (~4023) | zöld kell maradjon |

---

## 9. Bázis-effort allokáció

| Track | Fejlesztő-nap |
|-------|---------------|
| A: Domain + Application | ~7 nap |
| B: Infrastructure + Persistence | ~7 nap |
| C: Api + Realtime + Tests + Sidecar | ~5 nap |
| **Összesen** | **~19 nap** (4 párhuzamos agent-szállal) |

---

## 10. Layer-specifikus CLAUDE.md fájlok

Minden agent köteles a saját layer-specifikus CLAUDE.md-jét **olvasás előtt feldolgozni**. Ezek a fájlok a meglévő repo-ban élnek:

| Layer | CLAUDE.md hely | Tartalom |
|-------|----------------|----------|
| Domain | `src/SpaceOS.Modules.Cutting.Execution.Domain/CLAUDE.md` | DDD invariánsok, Golden Rule 1-3, no setter, no nav-prop |
| Application | `src/SpaceOS.Modules.Cutting.Execution.Application/CLAUDE.md` | CQRS, Result<T>, ConfigureAwait, FluentValidation boundary |
| Infrastructure | `src/SpaceOS.Modules.Cutting.Execution.Infrastructure/CLAUDE.md` | EF Core, RLS, Specification, OutboxInterceptor regisztráció |
| Api | `src/SpaceOS.Modules.Cutting.Execution.Api/CLAUDE.md` | Minimal API, JWT, OpenAPI snapshot |

**Minden új fájl tartalmazzon `// File: <path>` kommentet az első sorban.**

---

## 11. Roll-back terv

Ha kritikus bug 24 órán belül:

1. **REVOKE** `spaceos_app` INSERT joga az `CuttingExecutions`-en → új execution stop
2. **OutboxDispatcher stop** → SignalR fan-out szünet (outbox Pending marad)
3. **API service** rollback Phase 3-as binárra (DB schema kompatibilis, csak új táblák)
4. Migration rollback **csak végső esetben** (`pg_dump` előtte kötelező)

---

## 12. Mikor kész a Phase 4

Ha **minden DoD checkbox** zöld:
- §9.1 Migration gates (10 item)
- §9.2 Domain gates (10 item)
- §9.3 API + validation gates (12 item)
- §9.4 Real-time gates (6 item)
- §9.5 Cross-tenant gates (6 item)
- §9.6 Security gates (28 item — 12 csoport)
- §9.7 Backend gates (9 item — új v4)
- §9.8 Test gates (5 item: ≥ 210 új teszt + meglévő ~4023 zöld)
- §9.9 Összesített (16 item)

**Total: 102 deployment-blocker item.**

---

## 13. Mi jön ezután

| Phase | Mi indulhat Phase 4 zárás után |
|-------|-------------------------------|
| Cutting Phase 5: Analytics | Waste %, OEE, capacity, TPM provisioning |
| Cutting Phase 6: Adapters | OptiCut + external nesting |
| Manufacturing Phase 1 | Edge banding feliratkozik `PanelCompleted(1/N)`-re |
| Logistics Phase 1 | Dispatch feliratkozik `CuttingExecutionCompleted`-re |

---

*Phase 4 README · 2026-04-26 · v4 tervdokumentum belépési pont*
