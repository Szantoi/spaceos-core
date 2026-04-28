---
id: MSG-CABINET-008
from: root
to: cabinet
type: task
priority: high
status: READ
ref: SpaceOS_Cabinet_0_3_Federation_Architecture_v4.md
created: 2026-04-28
---

# CABINET-008 — Cabinet 0.3 Track A: DB + Domain (Day 1–5)

> **Tervdok:** `/opt/spaceos/docs/tasks/active/SpaceOS_Cabinet_0_3_Federation_Architecture_v4.md` — KÖTELEZŐ olvasmány!
> **README:** `/opt/spaceos/docs/tasks/active/PHASE_CABINET_03_README.md` — agent context
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** Cabinet 0.2 COMPLETE (518 teszt, git `3098a60`)
> **Használhatsz sub-agent-eket** ha szükséges

## FONTOS — Migration számsütközés

Az Architect review (ARCH-015) azonosította: a tervdok 0027–0030 migration számokat használ, de ezek a Kernel-ben már le vannak foglalva. **Használj 0031–0034 számozást**, vagy unnumbered naming-et (`Cabinet03_TenantStandard`, stb.) — a lényeg hogy ne ütközzön.

---

## Scope (tervdok Day 1–5)

### Day 1–2: TenantStandard aggregate

A tervdok §3 szerint:
- `TenantStandard` aggregate — module-scoped, egy tenant N db-ot tarthat
- `TenantStandardProfile` — anyag defaults, bore patterns, threshold config
- `TenantStandardRule` — tenant-specifikus construction rule-ok
- Domain events: TenantStandardCreated, ProfileUpdated, RuleAdded

### Day 3–4: CatalogEntry Federation bővítés

A tervdok §2 szerint:
- `CatalogEntry` bővítés: `Visibility.Shared` + `Visibility.Community`
- `SimilarityFingerprint` — `ICatalogFingerprintExtractor` + DB trigger (SEC-02)
- `CatalogEntryCluster` — fingerprint-alapú csoportosítás
- `CatalogEntryRating` — 1-5 csillag, per-tenant
- `CatalogEntryFlag` — community moderation flag

### Day 5: SnapshotMigrator + smoke

- `SnapshotMigrator_0_2_to_0_3` — forward-only, `AppliedTenantStandard = null` default
- Round-trip tesztek
- Reference snapshot `0.3.json`

---

## Tesztek (80+)

**TenantStandard (25+):** Create, Profile CRUD, Rule add/remove, FSM
**CatalogEntry Federation (25+):** Shared/Community visibility, fingerprint, cluster
**Rating + Flag (15+):** Rate, flag, moderation queue
**SnapshotMigrator (15+):** 0.2→0.3, round-trip, edge cases

## Definition of Done

- [ ] TenantStandard aggregate komplett
- [ ] CatalogEntry Federation (Shared + Community + Fingerprint + Cluster)
- [ ] Rating + Flag entities
- [ ] SnapshotMigrator_0_2_to_0_3
- [ ] `dotnet build -c Release` 0 error, 0 warning
- [ ] `dotnet test` ≥ 598 pass (518 előző + 80 új)
- [ ] net8.0 ÉS net10.0 PASS
- [ ] Outbox DONE
