# PHASE_CABINET_03_README.md — Claude Code agent context

> **Source:** `SpaceOS_Cabinet_0_3_Federation_Architecture_v4.md` (v4.0 IMPLEMENTÁCIÓRA KÉSZ)
> **Cél:** ez a fájl egy **Claude Code agent** context-fájla a VPS-en futó implementáció-agent-nek. NE módosítsd Claude Desktop / Claude.ai chat session-ben — a v4 dokumentum az igazság forrása.
> **Implementáció becsült effort:** ~13 fejlesztői nap
> **Multi-target:** `net8.0;net10.0`

---

## 0. Pre-flight check (kötelező sorrendben)

Mielőtt a Claude Code agent egyetlen fájlt is módosít:

```bash
# Cabinet 0.2 baseline verifikáció
cd /opt/spaceos/repos/spaceos-cabinet
git log --oneline -1   # MUST be: 3098a60 (Cabinet 0.2 COMPLETE)
dotnet test --filter "Category!=Slow"   # MUST: 518 passed, 0 failed

# Kernel monorepo baseline
cd /opt/spaceos/repos/spaceos-kernel
git status   # MUST: clean
dotnet test  # MUST: 4688 passed (Cabinet 0.2 + Kernel + Orchestrator + Portal kombinált)
```

Ha BÁRMELYIK lépés sikertelen → STOP, jelezd a felhasználónak, NE folytasd a Cabinet 0.3 munkát.

---

## 1. Implementációs sorrend (sprint napok)

### Day 1 — Pre-migration + Migration 0027 (TenantStandard)
- `bootstrap_cabinet_03_roles.sql` (SEC-01) — `cabinet_moderator_role`, `cabinet_system_actor_role` PG ROLE-ok
- Migration `0027_AddCabinetTenantStandard` — RLS, `cabinet_set_updated_at` SECURITY INVOKER trigger (SEC-07)
- `TenantStandard` aggregate (Cabinet.Domain) — `bigint Version` (DB-06), 7 domain event
- `TenantStandardRepository` (Cabinet.Catalog) — Ardalis.Specification
- 25 unit teszt: aggregate mutators, version mismatch → Result.Conflict, jsonb size guards (DB-10)

### Day 2-3 — Migration 0028 + CatalogEntry bővítés
- Migration `0028_AddCabinetCatalogCommunityFields` — `IsAutoHidden GENERATED` (DB-04 + SEC-03), `IdempotencyKey UQ partial` (DB-08), `numeric(3,2) AverageStars` (DB-02), tightened fingerprint regex (DB-14), `COLLATE "C"` (DB-12)
- Migration `0028 SEC-02 extension` — `cabinet_recompute_fingerprint()` BEFORE INSERT/UPDATE trigger
- `CatalogEntry` aggregate bővítés (Cabinet.Catalog): `SimilarityFingerprint`, `ClusterId`, `AdminAcknowledgedUntil`, `IngestRating`, `IngestFlag`, `RemoveByAdmin` (DB-05 cluster-canonical-reassign invariant)
- `ICatalogFingerprintExtractor` port (Cabinet.Abstractions) + `DefaultCatalogFingerprintExtractor` (Cabinet.Catalog) — 5 CatalogType-onként
- 35 unit teszt: fingerprint extraction (5 type × 7 case), auto-hide threshold, time-bounded ack 90 napos visszatérés (SEC-03)

### Day 4 — Migration 0029 (Cluster) + cluster aggregate
- Migration `0029_AddCabinetCatalogClusters` — UQ(Fingerprint, Type), `MemberCount` trigger-maintained (DB-03), bigint Version (DB-06)
- `CatalogEntryCluster` aggregate (Cabinet.Catalog): `CreateForEntry`, `AddMember`, `RemoveMember`, `RecomputeCanonical` 7-day probation szűrő (SEC-05)
- `ICatalogClusterRepository`
- 25 unit teszt: cluster auto-join concurrent insert (10 párhuzamos, 1 cluster + 10 member), canonical 7-day probation, flag-eligible szűrő

### Day 5 — Migration 0030 (Rating + Flag)
- Migration `0030_AddCabinetCatalogRatingsAndFlags` — RLS `rating_read_visible_community` (DB-01 CRITICAL), role-based `flag_read_admin_only` (SEC-01), aggregate-maintenance triggerek SECURITY INVOKER (DB-03 + SEC-07), self-rating + self-flag triggerek
- `CatalogEntryRating`, `CatalogEntryFlag` child entitások (Cabinet.Catalog) — SEC-13 PII regex-strip a Note/Comment-en
- `IRatingRepository`, `IFlagRepository`
- 20 unit teszt: self-rating/flag domain + DB layer két-réteg védelem; FlagState FSM 4 állapot

### Day 6 — Cabinet.Application command-ok + idempotent UPSERT
- `SubmitCommunityCatalogEntryCommand` + handler (BE-04 raw SQL UPSERT, ON CONFLICT, SEC-08 24h scope)
- `SubmitCatalogRatingCommand` + handler (DB-11 atomic UPSERT pattern, rate limit dual cap SEC-11)
- `SubmitCatalogFlagCommand` + handler (rate limit dual cap)
- `WithdrawCatalogRatingCommand`, `WithdrawCatalogFlagCommand`
- `AdminClearFlagsCommand` + handler (SEC-03 time-bounded ackUntil)
- `AdminRemoveCatalogEntryCommand` + handler (SEC-12 cluster ghost prevention)
- 15 unit teszt + 5 integráció-teszt (idempotent UPSERT race, dual rate limit)

### Day 7 — TenantStandard command-ok + Query oldal
- `EnsureTenantStandardCommand`, `Update*Command` (5 db, anyag/lineBore/thresholds/constructionDefaults/ruleSeverityOverride)
- `GetTenantStandardQuery`
- `GetCatalogClusterQuery`, `SearchCatalogClustersByFingerprintQuery`
- `GetCatalogRatingsQuery` (SEC-06: 200 OK + üres lista, soha 404)
- `ListAutoHiddenQuery` (admin only)
- 15 unit teszt

### Day 8 — Port-implementations (Infrastructure)
- `KernelShareLineageResolver` (Cabinet.Catalog → SpaceOS.Infrastructure.Cabinet) — `TenantHandshakeAllowlist` query
- `FlagModerationProvider` (Cabinet.Catalog) — `ClearFlagsAsync(TimeSpan? ackDuration)` time-bounded
- `CabinetRoleInterceptor` (DbCommandInterceptor) — `SET LOCAL ROLE cabinet_moderator_role` JWT claim alapján (SEC-01)
- `RedisRateLimitProvider` — sliding window `(TenantId, UserId)` és `(TenantId)` kettős cap (SEC-11)
- 10 integráció-teszt (Testcontainers PostgreSQL + Redis)

### Day 9 — ConstructionRuleEngine átírás (BE-01/02/03)
- `ConstructionRuleEngine.ApplyAllAsync` — Channel<T> pattern (BE-01), `MaxDegreeOfParallelism = Math.Min(ProcessorCount, 8)` (BE-02)
- Sync `ApplyAll` overload **TÖRÖLVE** (BE-03)
- `RuleContextFactory` — `FrozenDictionary<string, AdvisorySeverity>` pre-cache (BE-02)
- Roslyn analyzer: tilos `ITenantContext` injektálása `IConstructionRule`-ba (SEC-04); tilos `SimilarityFingerprint` setter Domain-on kívülről (SEC-02)
- 10 unit teszt + 5 property-based teszt (FsCheck): 100 random skeleton, 4 párhuzamos tenant context, no-leak invariant
- 1 BenchmarkDotNet projekt: ApplyAllAsync 50/100/500 Part skeleton-okon (BE-01 30% gyorsulás bizonyíték)

### Day 10 — SnapshotMigrator + Domain event handlers
- `SnapshotMigrator_0_2_to_0_3` (Cabinet.Domain) — forward-only, `AppliedTenantStandard = null` default
- 4 új event handler (BE-07): `ClusterMemberCountSyncHandler`, `FlagAuditLogHandler`, `ClusterMemberRemoveHandler`, `RatingHistoryResetHandler`
- 5 unit teszt (snapshot round-trip + reference snapshot equality)
- 5 integráció-teszt event handler chain (SEC-09 fingerprint-change → rating reset)

### Day 11 — Multi-target build + NuGet csomag-bumps
- Cabinet.Geometry, Cabinet.Machining, Cabinet.Assembly, Cabinet.Semantics → 0.2.1 patch (NuGet rebuild a 0.3.0 Abstractions ellen)
- Cabinet.Abstractions, Cabinet.Domain, Cabinet.Construction, Cabinet.Catalog, Cabinet.Application, Cabinet (meta) → 0.3.0
- `Microsoft.VisualStudio.Threading.Analyzers` 17.x minden Cabinet 0.3 csproj-be (BE-08)
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` minden Cabinet 0.3 csomagon
- DoD `grep ConfigureAwait` audit (BE-08): 0 hiányzó

### Day 12 — Test passes + CI verification
- Teljes test suite: meglévő 4688 + Cabinet 0.3 új 145 = **4833 tests** zöld
- Migration `Up → Down → Up` round-trip teszt staging-en (DB-13)
- BenchmarkDotNet baseline regisztráció a CI artifact-okba

### Day 13 — Documentation + release prep
- ADR-021 (TenantStandard module-scoped, NEM trade-type-bound — v1.1 framing)
- ADR-022 (Catalog Federation auto-publish + similarity-cluster — v1.1 architektúra)
- ADR-023 (Role-based RLS, NEM session-variable — SEC-01 CRITICAL)
- ADR-024 (Server-side fingerprint enforcement, hármas védelem — SEC-02 CRITICAL)
- Cabinet 0.3 release notes — CabinetBilder.Adapter.AutoCAD migration step (BE-03 sync overload removal)
- `docs/sample-snapshots/0.3.json` reference snapshot generation

---

## 2. Kritikus invariánsok — DoD blocker-ek

Mielőtt a `git tag v0.3.0` parancs futna, mind a 14 gate-nek zöldnek kell lennie:

1. **DB-01 RLS gate:** integráció-teszt — Private/Shared visibility entry rating-jét read-only role nem éri el
2. **DB-04 generated gate:** `INSERT INTO cabinet_catalog_entries (...IsAutoHidden...)` → SQL error
3. **SEC-01 CRITICAL gate:** penteszt — `SET LOCAL app.is_cabinet_moderator = 'true'` raw SQL `spaceos_application_user`-rel ➜ NEM ad hozzáférést a flag-ekhez
4. **SEC-02 CRITICAL gate:** Submit DTO-ban `SimilarityFingerprint` mező compile error; raw JSON-os spoof-attempt → DB trigger override + `audit_log_security_violations` row
5. **SEC-03 gate:** ClearFlagsByAdmin után 91 nappal új 3-flag wave → `IsAutoHidden = true` (időkorlát működik)
6. **SEC-04 gate:** property-based teszt 4 párhuzamos tenant `ApplyAllAsync` no-leak partíció
7. **SEC-07 gate:** `SELECT prosecdef FROM pg_proc WHERE proname LIKE 'cabinet_%'` → mind `false`
8. **BE-01 gate:** BenchmarkDotNet 500-Part skeleton ≥ 30% gyorsulás
9. **BE-04 gate:** 10 párhuzamos Submit ugyanazon `IdempotencyKey`-vel → 1 entry + 9 idempotent return + 0 `DbUpdateException`
10. **BE-06 gate:** `IngestRating` után `entry.Ratings.Count` IN-MEMORY pontos
11. **BE-08 gate:** `grep -r "await.*Async(" SpaceOS.Cabinet.* | grep -v ConfigureAwait` → 0
12. **DB-13 gate:** mind a 4 migration `Up → Down → Up` round-trip staging-en zöld
13. **Test count gate:** ≥ 145 új teszt + 4688 örökölt mind zöld
14. **Multi-target gate:** `net8.0` és `net10.0` build mindkettőn zöld

---

## 3. Modulokra osztott feladat-allokáció (Claude Code parallel agents)

A 13 napos sprint párhuzamosíthatóan futhat 3 Claude Code agent-tel:

| Agent | Felelősség | Day-szám |
|-------|-----------|----------|
| **Agent A — DB + Domain** | Migrations 0027–0030, TenantStandard, CatalogEntry, Cluster, Rating, Flag aggregate-ek | Day 1–5 |
| **Agent B — Application + Infrastructure** | Cabinet.Application command/query, Infrastructure port-implementations, RoleInterceptor, RateLimitProvider | Day 6–8 |
| **Agent C — RuleEngine + Tests + Polish** | ConstructionRuleEngine átírás, BenchmarkDotNet, Roslyn analyzer-ek, NuGet release prep | Day 9–13 |

A 3 agent egymástól függetlenül indul a Day 1-en, de a Day 6-os Application munka **a Day 5-ös Domain befejezésére vár** (cross-agent dependency). A párhuzamos terv ~6-7 napos wall-time-mal hozza a 13 fejlesztői napot.

---

## 4. Mit NE csináljon a Claude Code agent

| Tilos | Miért |
|-------|-------|
| `BuildServiceProvider()` használata Cabinet csomagokban | Anti-pattern (Cabinet 0.1 örökölt szabály); `IConfigureNamedOptions` és proper DI helyett |
| Sync `ApplyAll` overload visszakeverése | BE-03 explicit törölte; AutoCAD plugin upgrade-eljen |
| `SimilarityFingerprint` setter publikus tételel | SEC-02 CRITICAL; Roslyn analyzer ban |
| `ITenantContext` injektálása `IConstructionRule` constructor-ba | SEC-04; AsyncLocal-leak kockázat |
| `current_setting('app.is_cabinet_moderator', ...)` használata RLS-ben | SEC-01 CRITICAL; role-based-re migrált |
| `SECURITY DEFINER` PL/pgSQL function-ön | SEC-07 explicit `SECURITY INVOKER` minden Cabinet function-en |
| Outbox pattern bevezetése | BE-05 nem indokolt Cabinet 0.3-ban; Cabinet 0.4+ Marketplace dönti |
| `ConcurrentBag<T>` újra-bevezetése a RuleEngine-ben | BE-01 Channel-pattern-re lecserélte |
| Cabinet 0.2 örökölt `Modules.Joinery v4.2` használata | Deprecated; `Modules.Abstractions` használandó |

---

## 5. Cross-repo érintettség

| Repo | Változás Cabinet 0.3 release-szel |
|------|-----------------------------------|
| `spaceos-cabinet` | Fő implementáció, Cabinet 0.3.0 NuGet csomagok |
| `spaceos-kernel` | Migration-ok 0027-0030, `CabinetRoleInterceptor` regisztrálás, `bootstrap_cabinet_03_roles.sql` deploy step |
| `spaceos-cabinet-builder-adapter-autocad` | BE-03 sync overload eltávolítás → ~12 hívóhely `await ApplyAllAsync`-re refaktor; ~0.5 nap effort |
| `spaceos-doorstar-portal` | Community Catalog UI (rating, flag form, admin queue) — **NEM Cabinet 0.3 sprint, külön Portal phase** |
| `spaceos-asztalostech-portal` | Ugyanaz mint Doorstar, külön Portal phase |

---

## 6. Megelőző kommunikáció — érintett stakeholder-ek

| Stakeholder | Mit jelezni Day 1 előtt |
|-------------|-------------------------|
| Doorstar Kft. | Cabinet 0.3 a soft-launch előtt mehet, NEM blokkolja a Doorstar Soft Launch ütemtervet (Cabinet 0.3 fogyasztó: cabinet template builder, NEM door builder) |
| Asztalos Profi Kft. (új ICP) | Community Catalog auto-publish + cluster modell — első tenant szempontjából értékes (Egger / Hettich / Blum termékkatalógus crowd-sourced) |
| AutoCAD plugin user-ek | BE-03 breaking change a plugin commands-on (`async`); release notes + 1-page migration guide |

---

*Generated from `SpaceOS_Cabinet_0_3_Federation_Architecture_v4.md` — v4.0 IMPLEMENTÁCIÓRA KÉSZ · 2026-04-27*
*Final review state: 3🔴 + 18🟠 + 12🟡 + 3🟢 = 36 finding kumulált, mind beépítve · ~13 fejlesztői nap*
