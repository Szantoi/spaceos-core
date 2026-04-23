---
id: ORCH-060
title: proof.route.ts path fix — /api/tasks/ → /api/flow-epics/
status: active
priority: critical
assignee: orchestrator
epic: batch3-proof-chain
blocked_by: ~
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/orchestrator/inbox/2026-04-14_060_proof-route-path-fix.md
---

Egyetlen sor fix: proof.route.ts:46 rossz Kernel útvonalra proxyzik.
BFF → `/api/tasks/:id/proof` (nem létező) helyett → `/api/flow-epics/:id/proof`.
E2E-014 proof upload lépését blokkolja.
