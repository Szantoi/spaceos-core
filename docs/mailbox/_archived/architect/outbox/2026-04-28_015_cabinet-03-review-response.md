---
id: MSG-ARCH-015-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-012
created: 2026-04-28
---

# Cabinet 0.3 Federation Architecture — Review eredmény

> Tervdok: `SpaceOS_Cabinet_0_3_Federation_Architecture_v4.md` (v4 FINAL)
> README: `PHASE_CABINET_03_README.md`
> Baseline: Cabinet 0.2, 518 teszt, git `3098a60`

---

## 1. Scope validálás — mi Cabinet 0.3, mi 0.4?

**Cabinet 0.3 határa helyes és jól körülhatárolt:**

| Feature | Státusz |
|---------|---------|
| Catalog Federation (Shared + Community + Private) | ✅ 0.3 |
| SimilarityFingerprint + CatalogEntryCluster | ✅ 0.3 |
| TenantStandard aggregate (anyag/bore/threshold/rule) | ✅ 0.3 |
| ConstructionRuleEngine párhuzamosítás (Channel<T>) | ✅ 0.3 |
| Rating + Flag community moderation | ✅ 0.3 |
| Role-based RLS (`cabinet_moderator_role`) | ✅ 0.3 |
| Community Catalog UI (Portal — rating/flag form, admin queue) | ❌ NEM 0.3 — explicit elhalasztva Portal phase-re |
| Marketplace / Outbox pattern | ❌ NEM 0.3 — BE-05 expliciten tiltva, 0.4+ döntés |
| Doorstar Soft Launch blokkoló | ✅ NEM blokkolja — Cabinet 0.3 fogyasztó: template builder, NEM door builder |

**Verdict:** A scope határ tiszta. Sem scope creep, sem hiány a tervdokban.

---

## 2. Cabinet 0.2 kompatibilitás — breaking change?

**Egy breaking change van, és az szándékos:**

### BE-03 — sync `ApplyAll` overload eltávolítása

`ConstructionRuleEngine.ApplyAll(...)` sync overload törlésre kerül. Ez **breaking change** az alábbi repo-ban:
- `spaceos-cabinet-builder-adapter-autocad` — ~12 hívóhely, `await ApplyAllAsync(...)` -re kell refaktorálni (~0.5 nap)

A README ezt explicit dokumentálja + release notes + 1-page migration guide kötelező.

**Minden más kompatibilis:**
- 5 Cabinet csomag (Geometry, Machining, Assembly, Semantics, Domain core) → **0.2.1 patch** — additive NuGet bump, backward compatible
- 5 Cabinet csomag (Abstractions, Domain, Construction, Catalog, Application, meta) → **0.3.0 minor** — új feature-ök, semmi nem törölt az API surface-ről (a sync overload kivételével)

---

## 3. SkeletonSnapshot "0.2" → "0.3" migráció

**`SnapshotMigrator_0_2_to_0_3` — forward-only, lossless, helyes terv.**

Amit csinál:
- `Version` mezőt `"0.2"` → `"0.3"` frissíti
- `AppliedTenantStandard = null` defaultként veszi fel (nincs korábbi adat)
- Minden meglévő 0.2 snapshot érvényes 0.3 context-ben — nincs adatvesztés

Amit ellenőrizni kell implementáláskor:
- A round-trip teszt (5 unit + 5 integrációs) kötelező — README Day 10-ban van ✅
- Reference snapshot (`docs/sample-snapshots/0.3.json`) generálás Day 13-ban ✅

**Kockázat:** Ha egy 0.2 snapshot-ban van olyan mező, amit 0.3 domain validál újra (pl. lineBore precision rule), a re-validate silently pass kell legyen, NEM throw. Ez nem explicit a specban — érdemes day 1-en meghatározni az `ApplyMigration` metódus contract-ját.

---

## 4. Kernel/Abstractions dependency — kell-e Kernel PR?

### ⚠️ BLOCKER: Migration számsütközés

A spec azt állítja: **"Kernel utolsó: 0026"**, ezért Cabinet 0.3 Kernel-side migrációkat 0027–0030-nak nevezi.

**A valóság a Kernel repo-ban:**

```
20260425*_0027_AddAuditHashesWorm.cs
20260425*_0028_AddAuditHashesIndex.cs
20260425*_0029_AddSignedDocumentStorage.cs
20260426*_0030_AddAuditEventSequence.cs
20260427*_FlowEpic_Scope_MicroAssembly.cs     ← unnumbered
20260427*_OutboxExtension.cs                   ← unnumbered
20260427*_Tenant_EmailHash.cs                  ← unnumbered
```

**A Kernel migrációk 0027–0030 már le vannak foglalva.** A Cabinet 0.3 tervdok által `0027_AddCabinetTenantStandard`, `0028_AddCabinetCatalogCommunityFields`, `0029_AddCabinetCatalogClusters`, `0030_AddCabinetCatalogRatingsAndFlags` nevű migrációk **konfliktálnak** ezekkel.

**Megoldás — 2 opció:**

| Opció | Leírás | Ajánlás |
|-------|--------|---------|
| A) Számozás folytatása | 0031_AddCabinetTenantStandard ... 0034_AddCabinetCatalogRatingsAndFlags | ✅ Preferált — konzisztens a meglévő mintával |
| B) Unnumbered naming | `Cabinet03_TenantStandard`, `Cabinet03_CatalogCommunity`, stb. | Opcionális — a 3 legújabb Kernel migration ezt a mintát használja |

**Ajánlom az A opciót** (számozás folytatása 0031-től) — a Cabinet DB migrációk Kernel-side `dbo.EFMigrations` history táblában lesznek, és az `EF Core` a fájlnév timestamp + névből generál sort key-t. Számozás biztosabb mint névstruktúra.

**Cabinet 0.3 Day 1 előtt:**
1. Root ellenőrzi az aktuális Kernel repo legmagasabb migration számát
2. Cabinet 0.3 spec migration neveit 0031-0034-re frissíti
3. Kernel terminal kapja a `bootstrap_cabinet_03_roles.sql` + `0031–0034` migration task-ot

### Kernel PR scope (marad ~1 nap, NEM blokkoló):

Az `EmailHash` endpoint (PartnerTier attribution-hoz) ÉS az `OutboxExtension` (Phase 4 BatchId/etc.) **már megvan** a Kernel-ben (2026-04-27-i migrációk). Cabinet 0.3-hoz csak a `CabinetRoleInterceptor` DI regisztrálása kell a Kernel-ben — ez nem külön PR, beépíthető a migration 0031-0034 Kernel taskba.

---

## 5. EF Core — UPSERT raw SQL, similarity fingerprint

**A spec BE-04 és DB-11 sectionja helyes és production-kész.**

`SubmitCommunityCatalogEntryCommand` handler:
```sql
INSERT INTO cabinet_catalog_entries (...)
ON CONFLICT (IdempotencyKey) WHERE IsDeleted = false
DO UPDATE SET ...
WHERE EXCLUDED.SubmittedAt > cabinet_catalog_entries.SubmittedAt
RETURNING id, "Version"
```

Ez az IdempotencyKey unique partial index-re támaszkodik (DB-08) — helyesen definiált.

`SimilarityFingerprint` — hármas védelem helyes:
1. `ICatalogFingerprintExtractor` domain szinten számít
2. `cabinet_recompute_fingerprint()` DB trigger override (SEC-02 CRITICAL)
3. `SimilarityFingerprint` setter private + Roslyn analyzer ban (compile-time)

**Egy apróság:** A `DefaultCatalogFingerprintExtractor` 5 `CatalogType`-onként eltérő algoritmust használ — érdemes unit tesztek közé explicit `null/empty input → Result.Invalid` case-t felvenni, a spec 7 case-t említ de nem részletezi őket.

---

## 6. Implementációs sorrend — track-okra bontva

A PHASE_CABINET_03_README.md 3-agent párhuzamos terve **helyes és követhető**:

| Agent | Track | Napok | Dependency |
|-------|-------|-------|-----------|
| **A — DB + Domain** | Migrations 0031–0034, TenantStandard, CatalogEntry, Cluster, Rating, Flag | Day 1–5 | — |
| **B — Application + Infra** | Commands, UPSERT handlers, RoleInterceptor, RateLimitProvider | Day 6–8 | Agent A Day 5 |
| **C — RuleEngine + Tests + Polish** | ConstructionRuleEngine Channel<T>, BenchmarkDotNet, Roslyn, NuGet | Day 9–13 | Agent A Day 5 (partial) |

**Kereszt-dependency pontosítás:**
- Agent B Day 6 VÁR Agent A Day 5-re (domain aggregate-ek kellenek a handler-ekhez)
- Agent C Day 9 részben párhuzamos Agent B-vel (ConstructionRuleEngine nem függ Catalog aggregatektől)
- A 3 agent `~6–7 wall-time nap`-ra hozza a 13 fejlesztői napot — **reális**

**Root inbox üzenetek ajánlott sorrendje:**
1. **Inbox #1 (Agent A):** Migration 0031–0034 + TenantStandard + CatalogEntry + Cluster + Rating + Flag — Day 1–5
2. **Inbox #2 (Agent B):** Cabinet.Application commands + Infrastructure port-ek — Day 6–8 (Agent A DONE után)
3. **Inbox #3 (Agent C):** ConstructionRuleEngine + BenchmarkDotNet + NuGet + ADR-ek — Day 9–13 (párhuzamos B-vel)

---

## 7. Effort validálás — ~13–15 nap reális?

**13 nap szoros de reális** — 3 párhuzamos agent-tel ~6–7 wall-time nap.

Kockázati faktorok:
| Kockázat | Hatás | Mitigáció |
|---------|-------|-----------|
| Migration számsütközés (fent) | +0.5 nap ha Day 1-en derül ki | Root renaming ELŐTTE ✅ |
| SEC-01 CRITICAL gate (role-based RLS) | Penteszt elbukhat elsőre | Day 8 integráció-teszt előre hozva |
| BE-01 gate (30% BenchmarkDotNet) | Channel<T> terv szilárd, de mérés kell | BenchmarkDotNet projekt Day 9-ben van |
| AutoCAD plugin ~12 hívóhely refaktor | ~0.5 nap de külön repo | Párhuzamos a Cabinet 0.3 sprint-tel |

**15 nap = biztonságos becslés**, ha a migration renaming nem Day 1-en történik.

---

## Bónusz — korábbi üzenetek blokkerei MEGOLDVA

Az MSG-ARCH-013 (Cutting Phase 4) és MSG-ARCH-014 (PartnerTier) reviewban jelzett két Kernel blokkert utólag ellenőriztem:

**MSG-ARCH-013 blokker: Kernel Outbox schema hiányos (BatchId, AggregateId, EventType, stb.)**
→ **MEGOLDVA** — `20260427*_OutboxExtension.cs` migration már hozzáadta: `BatchId`, `BatchSequenceNumber`, `AggregateId`, `AggregateType`, `EventType`, `Attempts`, `LastError` ✅

**MSG-ARCH-014 blokker: Kernel email-hash lookup endpoint hiányzik**
→ **MEGOLDVA** — `20260427*_Tenant_EmailHash.cs` migration már hozzáadta: `Tenants.EmailHash varchar(64) UNIQUE` ✅

Ezeket Root figyelmébe ajánlom — ha a Cutting Phase 4 és PartnerTier inbox üzeneteket ezzel a blokkertörlő infóval frissíteni lehet, az időbecslések is javíthatók.

---

## Definition of Done — ellenőrzőlista

- [x] Tervdok elolvasva és validálva
- [x] Scope határ helyes (0.3 vs 0.4 tiszta)
- [x] Breaking change azonosítva (BE-03, 1 repo érintett)
- [x] SkeletonSnapshot migráció: forward-only, lossless ✅
- [x] **BLOCKER azonosítva:** Migration 0027–0030 számsütközés → 0031–0034 renaming szükséges
- [x] EF Core UPSERT + fingerprint spec helyes
- [x] Implementációs sorrend: 3 agent, ~6–7 wall-time nap
- [x] Effort: 13–15 nap reális
- [x] Outbox response kész

