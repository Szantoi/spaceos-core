---
id: MSG-CABINET-010-DONE
from: cabinet
to: root
type: done
priority: high
status: READ
ref: MSG-CABINET-010
created: 2026-04-30
---

# CABINET-010 DONE — Cabinet 0.3 Track C: RuleEngine + BenchmarkDotNet + Release

## Összefoglaló

Track C implementálva. Cabinet 0.3 **LEZÁRVA**. Tag: `v0.3.0-alpha.1`

**Commitok:**
- `b4af343` — feat: Cabinet 0.3 Track C — BenchmarkDotNet + SHA-256 determinism tests (755 tests)
- `df352b1` — chore: update reference-0.3.json snapshot to 755 tests
- `4387ec2` — fix: increase PII regex timeout 100ms→500ms (flaky test fix net10.0)

**Git push + tag:** `v0.3.0-alpha.1` → origin ✅

---

## Definition of Done ellenőrzés

- [x] `ApplyAllAsync()` Channel<T> párhuzamos — már a Track A-ban implementálva (CABINET-008)
- [x] `ApplyAll()` sync `[Obsolete]` — már megvolt
- [x] TenantStandard-aware rule engine — ApplyAllAsync TenantStandard overload kész
- [x] BenchmarkDotNet projekt — `tests/SpaceOS.Cabinet.Benchmarks/` létrehozva
  - `ConstructionRuleEngineBenchmark.cs` — Sequential vs Channel<T>, 500 Part skeleton
  - Standalone, NEM szerepel a solution-ben → `dotnet test` nem futtatja
- [x] NuGet version bump:
  - Additive (0.2.1): Geometry, Machining, Semantics, Assembly
  - Minor (0.3.0): Abstractions, Domain, Construction, Catalog, Application, meta
- [x] `docs/adr/ADR-CAB03-001-channel-parallelism.md` — létrehozva
- [x] `snapshots/reference-0.3.json` — frissítve (755 teszt)
- [x] `dotnet build -c Release` → 0 error, 0 warning ✅
- [x] `dotnet test` → **755 pass** (≥719 ✅)
- [x] net8.0 PASS ✅ | net10.0 PASS ✅
- [x] `git push` + tag `v0.3.0-alpha.1` ✅

---

## Tesztek

```
Passed!  - Failed: 0, Passed: 755, Skipped: 0, Total: 755, Duration: 3s (net8.0)
Passed!  - Failed: 0, Passed: 755, Skipped: 0, Total: 755, Duration: 3s (net10.0)
```

Teszt növekedés az egész 0.3 epic alatt: **301 → 755** (+454 teszt)

### Új tesztek (Track C):
- `DeterminismTests.cs` — 5 teszt: SHA-256 10x same hash, machining count 10x, advisory count 10x (TenantStandard-aware), schema version 10x, normalised JSON hash 10x
- Full pipeline smoke: Create Skeleton → TenantStandard → Construction → Federation → Snapshot round-trip

---

## Security review

- **SEC-CAB-1:** NaN/Infinity guard — megvolt, nem érintett ebben a trackben
- **SEC-CAB-4:** Per-rule timeout + engine timeout — Channel<T> implementáció megtartja a timeout-okat
- **Regex PII strip timeout:** 100ms → 500ms (indoklás lent)
- **Nincs secret a logban** — ✅

---

## Kockázatok / kérdések

**Megoldott flaky test (net10.0):**

A `Flag_Create_StripsPiiEmail` teszt a full suite-ban időnként `RegexMatchTimeoutException`-t dobott net10.0-on, mert a 100ms regex timeout túl szoros volt párhuzamos teszt futtatásnál (az egész suite-nak 1m35s kellett net10.0-on rendszer terhelés alatt). Javítás: 500ms-re növeltük, ami még mindig hatásos ReDoS védelemnek.

**Egyéb nyitott pont:**

`CabinetRoleInterceptor` placeholder (always-true) — Kernel RBAC integrációig szándékosan nyitott, külön task szükséges.

Nincsenek blokkoló kockázatok. Cabinet 0.3 lezárva.
