---
id: MSG-ARCH-016-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-013
created: 2026-04-28
---

# Cutting Phase 5 Analytics Architecture — Review eredmény

> Tervdok: `SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4.md` (v4 FINAL)
> README: `SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4_README.md`
> Baseline: Cutting Phase 4 DEPLOYED, 496 teszt (feltétel: DEPLOYED = igaz)

---

## 1. Scope — mi Phase 5, mi Phase 6?

**A határ tiszta és jól körülhatárolt:**

| Feature | Státusz |
|---------|---------|
| DailyExecutionMetric projekció (gép × nap) | ✅ Phase 5 |
| DailyMaterialUsage projekció (anyag × nap) | ✅ Phase 5 |
| MachineOEEHourly projekció | ✅ Phase 5 |
| DailyOperatorMetric + k-anon + l-diversity | ✅ Phase 5 |
| 5 analytics API endpoint + Dashboard + Rebuild | ✅ Phase 5 |
| Day-0 + new-tenant backfill (Outbox-pattern, BE-02) | ✅ Phase 5 |
| P4-4 Redis Sentinel generalizált rate-limiter | ✅ Phase 5 (deferred debt closure) |
| P4-9 TPM enrollment + per-tenant fallback policy | ✅ Phase 5 (deferred debt closure) |
| C-0015/C-0016 deprecated function drop | ❌ NEM Phase 5 — Phase 6-ra halasztva (BE-09 explicit) |
| OptiCut / külső nesting adapter | ❌ NEM Phase 5 — Phase 6 scope |
| Doorstar Portal Analytics Dashboard UI | ❌ NEM Phase 5 — külön Portal sprint (spec §12 explicit tiltja) |
| ML predictive / cross-tenant aggregátum | ❌ Phase 6+ (Modules.AnalyticsML) |

**Verdict:** Scope discipline teljes. Nincs scope creep, nincs hiány.

---

## 2. Phase 4 kompatibilitás — CuttingExecution aggregate-re épít?

**Igen, és jól illeszkedik. Egy backward-compat kockázat van, de kezelve.**

### Helyes rá-építés

Phase 5 nem módosítja a Phase 4 domain-t — a `CuttingExecution` FSM event-jeit (ExecutionStarted, ProgressRecorded, OffcutReported, ExecutionCompleted, ExecutionCancelled, WorkerConsentWithdrawn) olvassa ki az Outbox-ból és projiciálja saját `cutting_analytics` read-modeleibe. Ez teljesen tiszta — Phase 4 aggregates érintetlenek.

### BE-04 — backward-compat kockázat (kezelt)

A Phase 4 deployed environment-ben `IHandshakeRateLimiter` volt regisztrálva. A Phase 5 általánosított `IRateLimiter` interfészre való átállás **breaking change lett volna** — de az adapter pattern (BE-04) megakadályozza:

- `RedisSentinelRateLimiter` implementálja MIND `IRateLimiter`-t ÉS `IHandshakeRateLimiter`-t
- `RedisSentinelHandshakeRateLimiter` `[Obsolete]` wrapper marad 1 release-ig
- DI: két binding ugyanarra az instance-ra → Phase 4 deployed config startup-on átmegy

**Ez DoD gate — Phase 5 merge-előtt smoke test kötelező a deployed config-on.**

---

## 3. DB — `cutting_analytics` új séma vs. `spaceos_cutting` bővítés?

**Dedikált új séma — helyes döntés.**

| Szempont | Döntés |
|----------|--------|
| Schema neve | `cutting_analytics` (új, dedikált) |
| `spaceos_cutting` érintett? | NEM — olvasott via Outbox, nem bővített |
| Migration prefix | `C-0007` .. `C-0019` (folytatja Phase 4 `C-0006`-ot) |
| Migration conflict? | **NINCS** — Cutting modul saját EF Core context (`CuttingAnalyticsDbContext`), a Kernel-es migration számozástól független |

**Táblastruktúra (6 tábla + 1 view):**

| Tábla | Megjegyzés |
|-------|-----------|
| `daily_execution_metric` | RLS + FORCE RLS |
| `daily_material_usage` | RLS + FORCE RLS |
| `daily_operator_metric` | RLS + FORCE RLS, l-diversity expression index |
| `machine_oee_hourly` | RLS + FORCE RLS, AsSplitQuery |
| `analytics_rebuild_job` | Per-tenant 1 active korlát (SEC-07) |
| `processed_outbox_event` | Dedup ledger, 90 napos retention (SEC-03) |
| `daily_operator_metric_anonymized` | `WITH (security_barrier=true)` VIEW — `cutting_analytics_reader` csak ezen keresztül |

**SECURITY DEFINER megjegyzés:**

A `assert_anonymity_constraints` function (C-0019) `SECURITY DEFINER`-rel fut — ez tudatos és indokolt (a function a tenant-scoped `daily_operator_metric`-et olvassa `current_setting('app.tenant_id')`-vel szűrve, és a `cutting_analytics_reader` role-nak egyébként nincs direkta tábla SELECT joga). A `REVOKE ALL FROM PUBLIC` + szűk `GRANT TO cutting_app` kombináció megfelelő. Ez eltér a Cabinet 0.3 SEC-07 SECURITY INVOKER szabályától, de itt az izolált schema + minimális grant kontextusában helyes. ADR-025-be érdemes felvenni a különbséget.

---

## 4. Deferred-debt closure — P4-4 és P4-9

**Mindkettő teljesen lezárva Phase 5-ben.**

### P4-4 — Redis Sentinel

**Volt:** Phase 4-ben a Sentinel failover nem volt implementálva — csak single Redis node, `IHandshakeRateLimiter` szűk rate-limit.

**Zárás Phase 5-ben:**
- `IConnectionMultiplexer` Sentinel-aware konfigurálva (3 sentinel node docker-compose-ban)
- `RedisSentinelRateLimiter` — általános sliding window, per-tenant bucket
- `60 req/min standard + 5 concurrent HeavyQuery` analitika-specifikus limitek (SEC-05)
- BE-04 backward-compat adapter a Phase 4 handshake endpoint-hoz

### P4-9 — TPM enrollment

**Volt:** Phase 4-ben `ITpmKeyProvisioner` interfész + stub implementáció; éles TPM enrollment nem volt.

**Zárás Phase 5-ben:**
- `tpm2-pkcs11` könyvtár integráció (`TpmKeyProvisioner` valódi implementáció)
- `TpmFallbackPolicy` v3 — per-tenant opt-in, production default: **DISABLED** (SEC-02 CRITICAL)
- `KekFallbackProvisioner` two-slot KEK ha TPM unavailable + tenant opted-in
- `TpmAvailabilityMetricsCollector` — Prometheus `cutting_tpm_availability_ratio` + alert rule
- Stub provider dev/test környezetekre (`"Provider": "Stub"` appsettings.Development.json)

---

## 5. Implementációs sorrend — track-ok

**3 párhuzamos track, ~21 nap → ~8 wall-time nap.**

| Track | Fókusz | Napok | Kritikus dependency |
|-------|--------|-------|---------------------|
| **A — Domain + Application** | VO-k, read-model entity-k, aggregate, spec-ek, query handler-ek, projector-ek | Day 1–21 | — |
| **B — Infrastructure + Persistence** | Migrations C-0007..C-0019, DbContext, repository-k, RateLimiter, TPM | Day 1–21 | Track A Day 7 (IdempotencyGate interfész) |
| **C — API + BG Services + Tests** | Controller 7 endpoint, BackgroundService-ek, ≥115 teszt | Day 1–21 | Track A/B Day 9 (projector-ek, subscriber) |

**Kritikus cross-track dependency:**
- Track B Day 7-es `CuttingAnalyticsDbContext` kell Track A Day 7-es `ProjectionIdempotencyGate`-hez
- Track C Day 13 API integrációs tesztek Track A Day 13 controller-re várnak
- `TenantActivatedProjector` (BE-02, Day 9) a Kernel `OutboxExtension` migration-re épül — **ez már DEPLOYED** ✅

**Inbox bontás ajánlott sorrendje (3 inbox):**
1. **Inbox #1 (Track A):** Domain + Application teljes 21 nap
2. **Inbox #2 (Track B):** Infrastructure + Persistence teljes 21 nap (párhuzamos A-val)
3. **Inbox #3 (Track C):** API + BG Services + Tests teljes 21 nap (párhuzamos A/B-vel)

A 3 agent szinkronizáló pontok: Day 7 (Gate interfész), Day 9 (Subscriber), Day 13 (Controller).

---

## 6. Effort validálás

**~21.1 nap reális, 3 párhuzamos agent-tel ~8 wall-time nap.**

| Effort elem | Nap |
|-------------|-----|
| v1 alap | ~8.5 |
| DB review delta (+5.0) | |
| Security review delta (+4.1) | |
| Backend review delta (+3.5) | |
| **Összesen** | **~21.1** |

**4 review ciklus (49 finding) mind beépítve, 0 nyitott CRITICAL/HIGH.** A becslés konzervatív és szolid — Phase 4 precedens ~18 nap volt, Phase 5 kompexebb (analytics + P4-4/P4-9 debt + anonymizáció).

**Kockázati faktorok:**
| Kockázat | Hatás | Mitigáció |
|---------|-------|-----------|
| BE-04 startup crash ha deployed config másképp van | Production down | Smoke teszt deployed config-on DoD blocker |
| Day-0 backfill (~90 nap adat, chunked) | 24h-ig nincs analytics adat | Monitoring + runbook + `chunk_done/chunk_total` progress |
| `Contracts` NuGet v1.4.0 publish timing | Track C build fail Phase 5 nélkül | Project reference dev-time, NuGet publish Phase 5 indulásakor |
| TPM hardware VPS-en elérhető-e? | P4-9 éles csak stub-bal | `"Provider": "Stub"` ha nincs TPM — per-tenant enrollment dokumentált |

---

## 7. Cross-module dependency

**Minimális és tiszta:**

| Függőség | Típus | Státusz |
|----------|-------|---------|
| Kernel `OutboxExtension` (BatchId, AggregateId stb.) | Szükséges BE-02-höz | ✅ **DEPLOYED** (2026-04-27 migration) |
| `SpaceOS.Modules.Cutting.Contracts` NuGet v1.4.0 | Additive bump (3 új CapabilityFlag bit) | Új publish kell Phase 5 indulásakor |
| `SpaceOS.Nesting.Algorithms` NuGet | **NEM kell** — Phase 5 analytics, nem nesting | — |
| Kernel `TenantActivatedEvent` Outbox payload schema | Olvasott, nem módosított | ✅ Meglévő |
| `spaceos-doorstar-portal` | **NEM érintett** — Portal analytics dashboard külön sprint | — |

**Kernel Outbox blocker NINCS** — a Phase 4 review (MSG-ARCH-013) óta megtalált `OutboxExtension` migration (2026-04-27) már minden szükséges mezőt hozzáadott a Kernel `OutboxMessages` táblához.

---

## Definition of Done — ellenőrzőlista

- [x] Tervdok elolvasva és validálva (49 finding, 0 nyitott CRITICAL/HIGH)
- [x] Scope határ tiszta (Phase 5 vs Phase 6 vs Portal sprint)
- [x] Phase 4 kompatibilitás: BE-04 adapter pattern kezeli a backward-compat kockázatot
- [x] DB: dedikált `cutting_analytics` séma, C-0007..C-0019 — nincs migration konflikt
- [x] Deferred debt P4-4 + P4-9: mindkettő teljesen lezárva Phase 5-ben
- [x] Implementációs sorrend: 3 track × 21 nap → ~8 wall-time nap
- [x] Effort: ~21.1 nap reális
- [x] Cross-module deps: Kernel OutboxExtension ✅ DEPLOYED, Contracts v1.4.0 publish szükséges
- [x] Outbox response kész

