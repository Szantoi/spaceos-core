---
id: KERNEL-075
title: AuditEvents sequence BIGINT column — OccurredAt sort non-determinizmus fix
status: new
priority: high
assignee: kernel
epic: AUDIT-INTEGRITY
blocked_by:
created: 2026-04-15
updated: 2026-04-15
docs:
  - docs/mailbox/kernel/outbox/2026-04-15_074_sprint5-test-coverage-done.md
  - docs/risk-reviews/TestCoverageRiskReview_20260415.md
---

## Háttér

R-15 vizsgálat (KERNEL-074-DONE) azonosította: az `AuditEvents` tábla `GetChainAsync`
client-side OccurredAt szerint rendez. Azonos clock tick-en belül létrehozott eventek
nem-determinisztikus sorrendben kerülnek vissza → chain verification false positive
high-concurrency alatt.

**Jelenlegi mitigáció:** az AuditChainIntegrityTest 1ms delay-t használ a tesztek között.
Ez nem elegendő production-ban (pl. bulk import, B2B handshake burst).

## Feladat

1. `sequence BIGINT GENERATED ALWAYS AS IDENTITY` column hozzáadása az `AuditEvents` táblán
2. EF Core Migration (0030) generálása
3. `GetChainAsync` rendezés: `OccurredAt ASC, sequence ASC` (tiebreaker)
4. `AuditEventDispatcher` frissítés: `sequence` értéket nem kell explicit beállítani (DB generálja)
5. AuditChainIntegrityTest: 1ms delay eltávolítható, de maradhat is (redundáns védelem)
6. Összes teszt zöld

## Elfogadási kritériumok (DoD)

- [ ] Migration 0030 generálva és alkalmazható
- [ ] `GetChainAsync` sequence-alapú tiebreaker
- [ ] `AuditChain_*` tesztek 1ms delay nélkül is zöldek
- [ ] 1110+ teszt pass

## Prioritás indoklás

Q2 Soft Launch előtt fontos: Doorstar heavy usage (bulk product import, B2B handshake)
potenciálisan párhuzamos audit eventeket generál. False positive chain break = biztonsági
riasztás = support incident.
