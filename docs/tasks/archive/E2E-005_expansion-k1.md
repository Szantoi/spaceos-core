---
id: E2E-005
title: E2E bővítés Köteg 1 — Joinery · Stage Registry · Handshake · Abstractions
status: active
priority: high
assignee: e2e
epic: e2e-expansion
blocked_by: ""
created: 2026-04-12
updated: 2026-04-12
docs:
  - docs/mailbox/e2e/inbox/2026-04-12_005_expansion-k1-unblocked.md
---

# E2E-005 — Batch 1 expansion (unblokkolva 2026-04-12)

## Kontextus

Batch 1 prereq-ek teljesültek:
- Migration 0028 DB-ben ✅ (INFRA-060)
- Kernel binary c62f1d7 ✅ (INFRA-062)
- Joinery port 5002 ✅ (INFRA-061)
- Abstractions port 5003 ✅ (INFRA-061)

## Tesztek

| Fájl | Modul | Megjegyzés |
|---|---|---|
| `29-joinery-order.chain.test.ts` | Joinery (5002) | |
| `30-stage-registry.chain.test.ts` | Kernel Stage Registry | |
| `31-handshake-b2b.chain.test.ts` | B2B Handshake | |
| `34-abstractions.chain.test.ts` | Abstractions (5003) | ⚠️ Orchestrator proxy kérdéses |

## ⚠️ Kockázat

Az Orchestrator kódjában nincs `ABSTRACTIONS_BASE_URL` referencia. Ha a `34-abstractions` teszt BFF proxyt vár, 502-t kaphat. Jelzés: `MSG-E2E-005-BLOCKED-ORCH`.

## DoD

- [ ] 29-joinery-order pass
- [ ] 30-stage-registry pass
- [ ] 31-handshake-b2b pass
- [ ] 34-abstractions pass (vagy BLOCKED-ORCH jelzés)

## Mailbox

MSG-E2E-005 (kiadva: 2026-04-12) + MSG-E2E-005-UNBLOCKED (2026-04-12)
