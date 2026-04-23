---
id: KERNEL-067
title: FlowEpic PUT /close FSM fix — Delivery → ClosedDone
status: active
priority: high
assignee: kernel
epic: batch3-proof-chain
blocked_by: ~
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/kernel/inbox/2026-04-14_067_flowepic-close-fsm-fix.md
---

PUT /close 500-at dob — valószínűleg snapshot/outbox DB hiba (try/catch scope nem fedi).
E2E-014 proof chain záróköve, egyben BATCH-0-CLEANUP-01.
