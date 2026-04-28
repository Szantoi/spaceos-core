---
id: MSG-ARCH-018-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-015
created: 2026-04-28
---

# Cutting Phase 6 Adapters Architecture — Review eredmény

> Tervdok: `SpaceOS_Modules_Cutting_Phase6_Adapters_Architecture_v4.md` (v4 FINAL, 145KB)
> README: `SpaceOS_Modules_Cutting_Phase6_Adapters_Architecture_v4_README.md`
> 47 finding (3🔴/16🟠/21🟡/7🟢), 0 nyitott CRITICAL/HIGH

---

## 1. Scope — mi Phase 6, mi Phase 7?

**A határ tiszta és axiomákon alapul (A6-3: D-3 döntés).**

| Feature | Státusz |
|---------|---------|
| Adapter framework (`IExternalAdapterTransport`, `IAdapterFactory`, resolver) | ✅ Phase 6 |
| `OptiCutAdapter` + `OptiCutFormatConverter` (XXE-hardened) | ✅ Phase 6 |
| `CutRiteAdapter` + `CliWrapperTransport` (Wine subprocess) | ✅ Phase 6 |
| `ManualAdapter` (Submit-only, delegates Builtin) | ✅ Phase 6 |
| `TenantCuttingProviderConfig` aggregate + admin API (4 endpoint) | ✅ Phase 6 |
| Adapter audit log + health check + Polly resilience | ✅ Phase 6 |
| Builtin lift (`BuiltinCuttingProvider`) + `CuttingProviderService` delegation | ✅ Phase 6 (BE-02 backward compat) |
| External execution tracking (OptiCut/CutRite végrehajtás státusz pull) | ❌ NEM Phase 6 — D-3 döntés: hash chain + Stage Registry MINDIG SpaceOS-ban marad |
| Adapter SDK külső developer-eknek | ❌ NEM Phase 6 — Phase 7+ (post-Doorstar, csak ha ökoszisztéma igényeli) |
| OptiCut/CutRite real-time bidirectional sync | ❌ NEM Phase 6 — vendor nem támogatja file-based mode-ban |
| Inventory/Procurement adapter | ❌ NEM Phase 6 — saját modulok scope-ja |

**Doorstar Soft Launch szempontjából:** Manual + Builtin adapter kötelező (`joinerytech.hu` staging smoke test). OptiCut/CutRite vendor docs alapján, post-launch.

---

## 2. Phase 5 kompatibilitás — analytics-re épít?

**NEM épít Phase 5 analytics-re, csak a Phase 5 infrastruktúrát reuse-olja.**

| Szempont | Helyzet |
|----------|---------|
| `cutting_analytics` séma (Phase 5) | ❌ NEM érintett — Phase 6 a `spaceos_cutting` sémában marad |
| `CuttingAnalyticsDbContext` (Phase 5) | ❌ NEM érintett |
| Phase 5 Redis Sentinel | ✅ REUSE — `IDistributedCache` + pub/sub `adapter-config-changed:{tenantId}` channel (SEC-09) |
| Phase 5 OpenTelemetry pipeline | ✅ REUSE — `cutting.adapter.*` span tags hozzáadva (BE-15) |
| `CuttingDbContext` (Phase 1-5) | ✅ BŐVÍTÉS — 4 új DbSet + `TenantSessionInterceptor` automatikus RLS reuse |

### BE-02 — Builtin backward compat (kritikus!)

A Phase 1-5 `CuttingProviderService` **NEM törlődik**, hanem delegál az új `BuiltinCuttingProvider`-re:

```
Joinery/Cabinet/Window  →  CuttingProviderService (megmarad, [Obsolete] Phase 7)
                                    ↓ delegates
                           BuiltinCuttingProvider (új, Phase 6)
```

Ez azt jelenti: **Joinery, Cabinet, Window repók SEMMIT nem kell változtassanak Phase 6-ban.** A hivatkozás-csere (direkten `BuiltinCuttingProvider`-re) egy follow-up sprint, Phase 7 előtt.

**⚠️ Blokkoló feltétel: "Cutting Phase 1-5 DEPLOYED ✅" — Phase 5 nincs még deployolva.** Ugyanaz az issue mint Manufacturing Phase 1-nél. Phase 6 nem indulhat, amíg Phase 5 nincs DEPLOYED state-ben.

---

## 3. Adapter pattern — interfészek összefoglalója

**8 kulcs-interfész, jól elválasztott felelősségekkel:**

| Interfész | Réteg | Felelősség |
|-----------|-------|-----------|
| `ICuttingProvider` (Contracts v1.4.0) | Contract | Változatlan — Phase 6 nem bővíti a Contracts-ot |
| `ICuttingProviderResolver` | Application | Per-tenant, per-capability dispatch (runtime) |
| `IAdapterFactory` | Application | DI composition root — factory pattern, NEM `IServiceProvider` (BE-03 anti-pattern) |
| `IExternalAdapterTransport` | Infrastructure | 3 impl: `FileExchangeTransport`, `RestApiTransport`, `CliWrapperTransport` |
| `IAdapterFormatConverter<TIn,TOut>` | Adapter NuGet | Pure, no I/O, no DI — teljes unit-tesztelhető (A6-8 axióma) |
| `IConfigSecretDetector` | Application | Shannon entropy + regex — plaintext API key detection (SEC-06) |
| `IAdapterCallAuditWriter` | Infrastructure | Append-only audit row, sanitizált `error_message` (SEC-08) |
| `IBoundedSubprocessRunner` / `BoundedSubprocessRunner` | Infrastructure | cgroups v2 sandbox, argv-only spawn (SEC-05), 1MB stdout truncate (SEC-18) |

### Resolver dispatch logika (A6-1 axióma)

```
ResolveForCapabilityAsync(tenantId, CuttingSubmit)  → configured adapter (OptiCut/CutRite/Manual/Builtin)
ResolveForCapabilityAsync(tenantId, CuttingNesting) → configured adapter, ha capability megvan
ResolveForCapabilityAsync(tenantId, CuttingExecution) → ALWAYS Builtin (D-3 döntés)
ResolveForCapabilityAsync(tenantId, CuttingWaste)     → ALWAYS Builtin (D-3 döntés)
```

**Capability runtime double-check (SEC-04):** Resolver ellenőrzi `provider.Capabilities.HasFlag(required)` minden hívás előtt → ha hamis (capability spoof) → Builtin fallback + audit "capability_spoof_attempted" event.

---

## 4. Cross-module consumers — hogyan kapcsolódnak?

**Joinery, Cabinet, Window fogyasztók változatlanul működnek — BE-02 guarantee.**

| Fogyasztó | Jelenlegi hívás | Phase 6 után |
|-----------|----------------|-------------|
| `spaceos-modules-joinery` | `CuttingProviderService.SubmitAsync(...)` | Változatlan — service delegate-el `BuiltinCuttingProvider`-re |
| `spaceos-cabinet` | `CuttingProviderService.GetNestingResultAsync(...)` | Változatlan |
| Jövőbeli Window modul | `ICuttingProvider` contract-on | Resolver dispatching-en megy majd |

**Tenant-szintű adapter választás:** Az admin konfigurálja a tenant adapter-ét a Phase 6 admin API-n. Doorstar tenant például `Manual` adapter-t kap Soft Launch-ra (legelső), majd `OptiCut`-ot ha a vendor licensz megvan.

**Contracts NuGet bővítés:** Phase 6 NEM bővíti a Contracts-ot (`ICuttingProvider` v1.4.0 stabil). A 3 új adapter NuGet (`SpaceOS.Cutting.Adapter.OptiCut/CutRite/Manual`) szintén egymástól függetlenek.

---

## 5. Implementációs sorrend — track-ok

**6 sprint × ~2 nap = 13.5-16.5 nap — jól párhuzamosítható.**

| Sprint | Tartalom | Nap |
|--------|----------|-----|
| **S1 Foundation** | Migrations C-0020..C-0024 (lásd §7), domain aggregates, repository, EF config, `Result<T>` mapping | ~2 |
| **S2 Adapter framework core** | `IAdapterFactory`, 3 transport impl, `BoundedSubprocessRunner`, `TenantAdapterStorage`, Polly v8 | ~2 |
| **S3 Resolver + cross-cutting** | `ICuttingProviderResolver`, Redis cache invalidation, `IConfigSecretDetector`, `IAdapterCallAuditWriter`, `PollSchedulerBackgroundService` | ~2 |
| **S4 3 adapter NuGet** | `BuiltinCuttingProvider` (lift + delegate), `OptiCutAdapter` + converter, `CutRiteAdapter` + Wine, `ManualAdapter` | ~2 |
| **S5 API + integration** | 4 admin endpoint, MediatR pipeline order (BE-08), OpenAPI snapshot (BE-10), idempotency (BE-13), 145 teszt | ~2 |
| **S6 Hardening + ops** | pg_cron retention, `spaceos_cutting_retention` role, SBOM, EXPLAIN ANALYZE, staging smoke | ~2.5 |

**Egyetlen track elegendő** — a 6 sprint lineáris, nincs párhuzamos track kényszer (S1 az S2 prereqje, stb.). Ha 2 agent párhuzamos, S4 és S5 részlegesen futtatható S3 után.

**Inbox bontás ajánlott:**
1. **Inbox #1**: S1-S3 (foundation + framework + resolver) — ~6 nap
2. **Inbox #2**: S4-S5 (adapters + API + tesztek) — ~4 nap
3. **Inbox #3**: S6 (hardening + ops) — ~2.5 nap

---

## 6. Effort validálás — ~13.5-16.5 nap reális?

**13.5 nap szoros de reális. 16.5 nap biztonságos.** A 4 review ciklus (47 finding) mind absorbálva, 0 nyitott CRITICAL/HIGH. A 3 CRITICAL finding (SEC-01 path traversal, SEC-02 XXE, SEC-03 SSRF) teljes védelmi mélységgel le van kezelve.

**Doorstar Soft Launch szempontjából:** a Phase 6 Manual + Builtin adapter (~S1-S4 részhalmaza) elegendő a Soft Launch Gate-hez. OptiCut/CutRite vendor-specific részek (S4 végén) a Soft Launch után is implementálhatók.

---

## 7. Nincs új port (5005 marad) — séma bővítés

**Port: 5005 változatlan ✅**

**Séma: `spaceos_cutting` (nem új séma)** — a meglévő Cutting séma 5 új táblával bővül:

| Migration | Tábla |
|-----------|-------|
| C-0020 | `tenant_cutting_provider_config` |
| C-0021 | `tenant_cutting_provider_config_history` |
| C-0022 | `adapter_call_audit` (RANGE PARTITION) |
| C-0023 | `adapter_health_record` |
| C-0024 | `uuidv7()` SQL function helper |

### ⚠️ NAMING AMBIGUITY: Migration prefix átszámozás szükséges

**A spec eredeti prefix-e: C-0017..C-0021.** Ez ütközik a Phase 5 analytics-szel:

| Prefix | Phase 5 Analytics (`CuttingAnalyticsDbContext`) | Phase 6 Adapters (`CuttingDbContext`) |
|--------|------------------------------------------------|---------------------------------------|
| C-0017 | `DailyOperatorMetricAnonymizedView` | `tenant_cutting_provider_config` |
| C-0018 | `AnalyticsRebuildJobPerTenantConstraint` | `tenant_cutting_provider_config_history` |
| C-0019 | `CombinedAnonymityFunction` | `adapter_call_audit` |

**Nincs hard EF Core konflikt** — különböző DbContext, különböző `__EFMigrationsHistory` record-ok. DE: egy agent könnyen megzavarodhat a számokon, és rossz `--context` flag-gel futtathatja a migrationt.

**Ajánlás: Phase 6 migrations prefixe legyen C-0020..C-0024** (Phase 4 CuttingDbContext utolsó: C-0006, Phase 5 analytics legmagasabb: C-0019 → Phase 6 adapter folytatja 0020-tól). Az implementációs inbox üzenetbe ezt explicit meg kell adni.

**Kötelező migration run parancs (ügynök context-je):**
```bash
dotnet ef migrations add C-0020_AdapterProviderConfig \
    --context CuttingDbContext \          # KÖTELEZŐ -- nem CuttingAnalyticsDbContext
    --project Infrastructure \
    --startup-project Api
```

---

## Definition of Done — ellenőrzőlista

- [x] Tervdok elolvasva és validálva (47 finding, 0 nyitott CRITICAL/HIGH)
- [x] Scope határ: Phase 6 vs Phase 7 tiszta (D-3 döntés: execution ALWAYS Builtin)
- [x] Phase 5 compat: BE-02 delegation pattern, `CuttingProviderService` megmarad
- [x] Adapter interfészek: 8 kulcs-interfész dokumentálva, felelősségek tiszták
- [x] Cross-module fogyasztók (Joinery/Cabinet/Window): változatlanok maradnak
- [x] Implementációs sorrend: 6 sprint × 2 nap, inbox 3 üzenet
- [x] Effort: 13.5-16.5 nap reális
- [x] Port: 5005 változatlan ✅
- [x] Séma: `spaceos_cutting` bővítés, 5 új tábla
- [x] **NAMING AMBIGUITY**: C-0017..C-0021 → **C-0020..C-0024** átnevezés szükséges; `--context CuttingDbContext` flag kötelező
- [x] **BLOKKOLÓ**: "Cutting Phase 1-5 DEPLOYED ✅" a spec header-ben — Phase 5 NINCS MÉG DEPLOYED

