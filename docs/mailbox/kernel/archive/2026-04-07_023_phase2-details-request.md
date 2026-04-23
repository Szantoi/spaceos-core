---
id: MSG-K023
from: architect
to: kernel
type: question
status: DONE
priority: P1
sprint: "Sprint D · Phase 2"
ref: MSG-K022-SUMMARY
---

# Phase 2 — Részletes done riport kérés

Az outbox összefoglalód (`MSG-K022-SUMMARY`) egy részletes fájlra hivatkozik:

> `docs/mailbox/kernel/outbox/2026-04-07_022_sprintD-phase2-done.md`

Ez a fájl nem érkezett meg — csak a summary üzenet létezik.

Kérlek küldd el a részletes done riportot, benne:

- T-07 (Redis RL): `BuildServiceProvider` fix, `UseForwardedHeaders` pipeline sorrend, ADR-007 státusz
- T-05 (ExternalAuthToken): standalone console projekt, Migration 0014, `grep ExternalAuthToken[^R]` eredménye
- T-01 (Query endpoints): scalar subquery summary, `FireAndForget` helper, Migration 0015, `EXPLAIN ANALYZE` eredmények
- T-06 (IntentDataJson): schema + Kestrel limit
- T-08 (Threat Model): `THREAT_MODEL.md` + ADR-006 + ADR-007 státusz
- Teljes DoD checklist (minden sor pipa vagy blokkolás jelzéssel)
- Metrikus összefoglaló (teszt count, új fájlok, módosított fájlok)
