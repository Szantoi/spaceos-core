# SpaceOS — Cutting Phase 6 Adapters · Implementation Context (Claude Code Agent README)

> **Companion file:** `SpaceOS_Modules_Cutting_Phase6_Adapters_Architecture_v4.md` (a teljes terv)
> **Cél:** Ez a fájl egy Claude Code agent (Claude Opus 4.7, `claude-opus-4-7`) számára adja meg a fókuszált, akcionálható kontextust a Phase 6 implementáció megkezdéséhez. NEM a teljes architektúra-dokumentum — csak az "ami közvetlenül kell az induláshoz" + a v4-ben lezárt findingek "miért így" indoklása.
> **Olvasási sorrend agent-nek:** (1) ezt a README-t, (2) a v4 architecture document `§3.4 Domain aggregate model`, `§4 Adapter framework`, `§7 DB schema`, `§9 EF Core configuration`, és `§10 Definition of Done` szakaszait, (3) a `Cutting Phase 1-5` `CLAUDE.md`-t a `spaceos-modules-cutting/CLAUDE.md`-ben.

---

## 1. Egy mondatos összefoglaló

A Phase 6 az `ICuttingProvider` Contract három új implementációját szállítja — **OptiCut, CutRite, Manual** — egy közös adapter framework + tenant-szintű runtime resolver mögött, úgy hogy a meglévő Cutting Phase 1-5 chain (`Builtin`) változatlanul fut tovább, és a hash chain + execution FSM **mindig** SpaceOS-ban marad (külső execution delegation Phase 7+).

---

## 2. Mit kell létrehozni (file inventory)

### 2.1 Új projektek a `spaceos-modules-cutting/` repo-ban

| Projekt | Típus | Cél |
|---------|-------|-----|
| `SpaceOS.Cutting.Adapter.OptiCut` | NuGet library | OptiCutAdapter + OptiCutFormatConverter + DI extension |
| `SpaceOS.Cutting.Adapter.CutRite` | NuGet library | CutRiteAdapter + CutRiteFormatConverter + DI extension |
| `SpaceOS.Cutting.Adapter.Manual` | NuGet library | ManualAdapter (delegates to Builtin Submit-only path) |

### 2.2 Új namespace-ek a meglévő projektekben

| Namespace | Hely | Új class-ok |
|-----------|------|-------------|
| `Domain.Adapters` | `SpaceOS.Modules.Cutting.Domain/Adapters/` | `TenantCuttingProviderConfig`, `AdapterHealthRecord` (aggregates) + 5 domain event |
| `Application.Adapters` | `SpaceOS.Modules.Cutting.Application/Adapters/` | Resolver, factory, repos interfaces, specifications, commands, queries, secret detector, sanitizer |
| `Infrastructure.Adapters` | `SpaceOS.Modules.Cutting.Infrastructure/Adapters/` | EF entity configurations, repos, transports (3 db), background services, file storage, format converters |
| `Api.Endpoints.Admin` | `SpaceOS.Modules.Cutting.Api/Endpoints/` | `AdminAdapterEndpoints.cs` (4 endpoint) |

### 2.3 Új DB migrations

| Migration | Tábla | Súlyú gate |
|-----------|-------|------------|
| `C-0017` | `tenant_cutting_provider_config` | RLS, version optimistic lock, config_schema_version |
| `C-0018` | `tenant_cutting_provider_config_history` | FK ON DELETE RESTRICT DEFERRABLE, append-only + backdate prevention triggers |
| `C-0019` | `adapter_call_audit` | RANGE PARTITION (started_at), allowlisted UPDATE trigger, 6 initial partitions |
| `C-0020` | `adapter_health_record` | RLS, composite PK, last_success_at |
| `C-0021` | `uuidv7()` SQL function | helper for time-ordered UUID inserts |

### 2.4 pg_cron job

`operations/db_jobs/audit_retention.sql` — havi partíció auto-create + 90/730 napos cleanup, dedikált `spaceos_cutting_retention` role-on.

---

## 3. Implementálási sorrend (suggested by senior-pm)

A v4 §10 DoD nem definiálja a konkrét sorrendet, de a függőségi gráf alapján az alábbi **5 sprintbe** osztva ajánlott:

### Sprint 1 (~2 nap): Foundation
1. EF Core entity configurations + 5 új DB migration (C-0017..C-0021)
2. Domain aggregates (`TenantCuttingProviderConfig`, `AdapterHealthRecord`) + 5 domain event
3. Repository implementations + 3 specifikáció (BE-05)
4. Egységes `Result<T>` mapping + ProblemDetails (BE-12, BE-14)

### Sprint 2 (~2 nap): Adapter framework core
1. `IAdapterFactory` (BE-03) + `IAdapterRegistration` DI extensions
2. `IExternalAdapterTransport` + 3 implementáció (FileExchange / RestApi / CliWrapper)
3. `BoundedSubprocessRunner` (cgroups v2 + argv-only spawn, SEC-05/SEC-18)
4. `TenantAdapterStorage` (SEC-01 path canonicalization)
5. Polly v8 ResiliencePipeline (BE-06)

### Sprint 3 (~2 nap): Resolver + cross-cutting
1. `ICuttingProviderResolver` + capability double-check (SEC-04)
2. `IDistributedCache` Redis + pub/sub invalidation (SEC-09)
3. `IConfigSecretDetector` (SEC-06) + entropy check
4. `IAdapterCallAuditWriter` + sanitization (SEC-08) + OTel spans (BE-15)
5. `PollSchedulerBackgroundService` (SEC-10) + bounded channel (BE-07) + IServiceScopeFactory (BE-04)

### Sprint 4 (~2 nap): 3 adapter NuGet
1. `BuiltinCuttingProvider` új class — backward compat (BE-02): `CuttingProviderService` delegates
2. `OptiCutAdapter` + `OptiCutFormatConverter` (XXE-hardened, SEC-02) + golden file tests
3. `CutRiteAdapter` + Wine subprocess wrapper (CliWrapperTransport)
4. `ManualAdapter` (Submit-only, delegates to Builtin)

### Sprint 5 (~2 nap): API + integration tests
1. 4 admin endpoint (`AdminAdapterEndpoints.cs`)
2. MediatR pipeline behaviors (BE-08): RequestLogging → Validation → Authorization → AdvisoryLock → Transaction → Audit
3. OpenAPI snapshot test (BE-10) — `verified/admin-adapters-openapi.v1.json`
4. Idempotency-Key middleware (BE-13)
5. End-to-end smoke + 145 unit/integration test target zöldre

### Sprint 6 (~2.5 nap, optional buffer): Hardening + ops
1. `pg_cron` retention setup + `spaceos_cutting_retention` role (SEC-07)
2. SBOM + `packages.lock.json` (SEC-15) + Dependabot
3. OWASP top 10 önaudit checklist
4. EXPLAIN ANALYZE verification a partíción
5. Doorstar staging deploy + smoke tests

**Sprint összesen:** ~13.5–16.5 nap (egyezik a v4 effort becsléssel).

---

## 4. Top 10 implementációs gotcha (ezek a leggyakoribb hibák új SpaceOS modul build során)

| # | Gotcha | Megoldás |
|---|--------|----------|
| 1 | `IServiceProvider` injekt egy resolver-be (anti-pattern, BE-03) | `IAdapterFactory` absztrakció — `IEnumerable<KeyedAdapterRegistration>` a constructor-ban |
| 2 | `BackgroundService` direct injektál `DbContext`-et → halott context (BE-04) | `IServiceScopeFactory` injekt + `using var scope = _scopeFactory.CreateAsyncScope()` minden iterációban |
| 3 | `DateTimeOffset.UtcNow` direct hív (BE-09) | `TimeProvider` DI-ban, fake-elhető tesztben `FakeTimeProvider`-rel |
| 4 | `ConfigureAwait(false)` hiányzik production async hívásokon (Golden Rule #7) | Roslyn analyzer `Microsoft.VisualStudio.Threading.Analyzers.VSTHRD111` engedélyezve |
| 5 | `Result.Error` használat validation hibára (BE-12) | `Result.Invalid(IEnumerable<ValidationError>)` validation-ra; mapping táblázat §9.6 |
| 6 | `Polly v7 IAsyncPolicy` chain (BE-06) | `Polly v8 ResiliencePipeline` `AddResilienceHandler` API-val |
| 7 | XML parsing default `XmlDocument` (XXE risk, SEC-02) | `XmlReaderSettings { DtdProcessing = Prohibit, XmlResolver = null }` |
| 8 | Subprocess `Arguments` string (argv injection, SEC-05) | `Process.StartInfo.ArgumentList.Add(...)` (NEM `Arguments`), `UseShellExecute=false` |
| 9 | Filesystem path concat user input-tal (path traversal, SEC-01) | `Path.GetFullPath(...).StartsWith(canonicalRoot)` runtime check minden I/O-nál |
| 10 | `IMemoryCache` multi-instance deploy-ban (cache poisoning, SEC-09) | `IDistributedCache` Redis-szel + pub/sub invalidation `adapter-config-changed:{tenantId}` channel |

---

## 5. Hivatkozott Phase 1-5 komponensek (reuse, NEM újra-implementálni)

| Komponens | Hely | Phase 6 felhasználás |
|-----------|------|----------------------|
| `CuttingDbContext` | `SpaceOS.Modules.Cutting.Infrastructure/Data/` | 4 új DbSet hozzáadva, RLS interceptor reuse |
| `TenantSessionInterceptor` | `SpaceOS.Modules.Cutting.Infrastructure/Data/Interceptors/` | Automatikusan applies az új táblákra |
| `CuttingProviderService` | `SpaceOS.Modules.Cutting.Application/Services/` | **Megmarad** + delegál `BuiltinCuttingProvider`-re (BE-02 backward compat) |
| `IRepositoryBase<T>` (Ardalis) | `SpaceOS.Modules.Cutting.Application/Common/` | Új repo-k öröklik |
| Phase 5 OpenTelemetry pipeline | `SpaceOS.Modules.Cutting.Api/Telemetry/` | OTel span tags `cutting.adapter.*` (BE-15) |
| Phase 5 Redis Sentinel | `infra/redis/` | `IDistributedCache` + pub/sub reuse (SEC-09) |
| Kernel `IDomainEventDispatcher` | `SpaceOS.Kernel.Application/` | 5 új domain event dispatchel (Golden Rule #4) |

---

## 6. Tesztek minimum target-tje

- **Unit + integration tests:** ≥ 145 db (lásd §10.8 v4)
- **Architecture tests** (`NetArchTest.Rules`):
  - Domain projekt nem hivatkozik MediatR/EF Core/Infrastructure-re
  - Resolver és handler-ek nem injektálnak `IServiceProvider`-t (BE-03)
  - `BackgroundService` impl-ek `IServiceScopeFactory`-t injektálnak (BE-04)
- **OpenAPI snapshot test** — `verified/admin-adapters-openapi.v1.json` (BE-10)
- **Negative security tests** (kötelező):
  - SEC-01: cross-tenant path injection → exception
  - SEC-02: XXE / billion laughs payload → parse rejection
  - SEC-03: cloud metadata IP / localhost → SSRF rejection
  - SEC-04: `MockSpoofingAdapter` capability spoof → Builtin fallback + audit event
  - SEC-05: malicious filename `"; rm -rf /"` → literal arg, no shell exec
  - SEC-06: plaintext API key in config_json → 400 Bad Request
- **DB integration tests**:
  - C-0018 backdating prevention (DB-02)
  - C-0019 allowlisted UPDATE trigger (DB-12)
  - C-0017 optimistic locking version conflict (DB-08, DB-13)

---

## 7. Mit kérdezzen az agent ha bizonytalan?

| Kérdéstípus | Hova fordul |
|-------------|-------------|
| "OptiCut XML format konkrét?" | `SpaceOS_Modules_Cutting_Vision_v1.md` §4 + vendor docs (Phase 6 implementáció során írandó) |
| "Phase 1-5 `CuttingProviderService` interface?" | `SpaceOS.Modules.Cutting.Application/Services/CuttingProviderService.cs` |
| "Hash chain érintve van?" | NEM — Phase 6 nem érinti a hash chain-t (D-3 döntés, csak nesting delegáció) |
| "Joinery / Cabinet handler-ek hivatkoznak `CuttingProviderService`-re?" | IGEN — BE-02 backward compat miatt megmarad. Hivatkozás-csere out-of-scope, follow-up sprintben (§12 lépés #6) |
| "RLS `app.current_tenant_id` session var hol állítódik?" | `TenantSessionInterceptor` Phase 1-5-ben — automatikusan applies |

---

## 8. Mi NINCS a Phase 6 scope-jában (gyakori scope creep témák)

- ❌ External execution tracking (capability spoof prevention miatt, D-3 döntés)
- ❌ OptiCut/CutRite real-time bidirectional sync (vendor nem támogatja)
- ❌ Adapter SDK külső developer-eknek (Phase 7+)
- ❌ Inventory/Procurement adapter (külön modulok lesznek)
- ❌ Builtin Phase 1-5 új feature (külön sprint)
- ❌ Joinery/Cabinet hivatkozás-csere `BuiltinCuttingProvider`-re (BE-02 follow-up)
- ❌ S3 Object Lock WORM offload (Escrow GA gate, Phase 6 csak retention policy-t definál)
- ❌ GDPR pseudonymization (P2-3 future)

---

## 9. Súlyos finding-listák gyors reference (a fix kód helye a v4-ben)

| Súly | Finding | v4 §-szám |
|------|---------|-----------|
| 🔴 SEC-01 path traversal | `WriteToInboxAsync` canonical-path containment | §4.3.4 |
| 🔴 SEC-02 XXE | `OptiCutFormatConverter` XmlReaderSettings | §5.1 |
| 🔴 SEC-03 SSRF | `RestApiTransport` IP allowlist + DNS rebinding | §4.3.4 |
| 🟠 SEC-04 capability spoof | `CuttingProviderResolver.ResolveForCapabilityAsync` runtime check | §4.4 |
| 🟠 SEC-05 argv injection | `BoundedSubprocessRunner.ArgumentList` | §6.5 |
| 🟠 SEC-06 plaintext secret | `IConfigSecretDetector` regex + entropy | §6.6 |
| 🟠 SEC-07 pg_cron privilege | `spaceos_cutting_retention` role + event trigger | §6.7 |
| 🟠 SEC-08 log injection | `AuditSanitizer.Sanitize` | §6.2 |
| 🟠 SEC-09 cache poisoning | Redis pub/sub `adapter-config-changed:` channel | §4.4 |
| 🟠 SEC-10 thread starvation | `PollSchedulerBackgroundService` bounded channel | §6.8 |
| 🟠 BE-01 aggregate sealing | `Domain/Adapters/` namespace | §3.4 |
| 🟠 BE-02 Builtin lift breaking change | `BuiltinCuttingProvider` új class + delegation | §10.4 |
| 🟠 BE-03 IServiceProvider antipattern | `IAdapterFactory` absztrakció | §4.4 |
| 🟠 BE-04 BackgroundService scope | `IServiceScopeFactory` per iteration | §6.8 + Resolver |
| 🟠 DB-01 history FK | `ON DELETE RESTRICT DEFERRABLE` | §7.2 C-0018 |
| 🟠 DB-02 history backdating | `chk_changed_at_not_future` + BEFORE INSERT trigger | §7.2 C-0018 |
| 🟠 DB-04 audit unbounded growth | `RANGE PARTITION (started_at)` + pg_cron retention | §7.2 C-0019 + §7.5 |
| 🟠 DB-08 concurrent admin update | `pg_advisory_xact_lock` + `version` optimistic lock | §10.5 + §3.4 |
| 🟠 DB-12 audit tamper resistance | allowlisted-UPDATE trigger | §7.2 C-0019 |

---

## 10. Mikor készen van a Phase 6?

- [ ] §10 DoD minden gate ✅ (10.1 .. 10.9)
- [ ] OWASP ZAP/Burp pentest staging-en sikeres
- [ ] Doorstar Soft Launch tenant smoke test: configure OptiCut adapter → submit cutting sheet → poll nesting result → success
- [ ] Cutting Phase 1-5 regression test suite zöldre fut (Joinery + Cabinet flow change-mentes)
- [ ] DBA aláírja a §7.5 sizing analysis-t

**Production gate (Doorstar GA):** A Phase 6 implementáció DEPLOYED state-be kerül a `joinerytech.hu` portál Doorstar tenant-jén, manual + builtin adapter-konfigurációval **kötelező**. OptiCut/CutRite implementáció a Soft Launch utáni szakaszban kerül engedélyezésre, vendor docs alapján.

---

*SpaceOS Cutting Phase 6 Adapters · Implementation README v4.0*
*Companion to: `SpaceOS_Modules_Cutting_Phase6_Adapters_Architecture_v4.md`*
*2026-04-28 · Status: Implementation-ready ✅*
