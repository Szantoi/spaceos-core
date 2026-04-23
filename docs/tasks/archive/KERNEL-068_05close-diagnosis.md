---
id: KERNEL-068
title: 05-close FSM E2E diagnózis — miért fail 46d6352 után?
status: active
priority: high
assignee: kernel
epic: BATCH-0-CLEANUP
blocked_by:
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/kernel/inbox/2026-04-14_068_05close-e2e-diagnosis.md
  - docs/mailbox/e2e/outbox/2026-04-14_015_rerun-plus-37-tools-done.md
---

## Feladat

Diagnózis: `05-flowepic-lifecycle` E2E teszt (PUT /close) miért fail-el a KERNEL-067 RLS fix (46d6352) után is?

**Lehetséges okok:**
1. Cascade: POST /api/flow-epics 500 → close lépés soha nem fut
2. Deploy probléma: 46d6352 DLL valójában nem fut
3. Más bug a close chain-ben

**Output:** DONE (ha fix kell) vagy BLOCKED (ha más terminál felelős).
