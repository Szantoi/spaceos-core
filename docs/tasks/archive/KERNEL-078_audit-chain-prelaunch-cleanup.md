---
id: KERNEL-078
title: Pre-launch audit chain cleanup — teszt tenant events törlése prod DB-ből
status: active
priority: medium
assignee: kernel
epic: AUDIT-INTEGRITY
blocked_by:
created: 2026-04-15
updated: 2026-04-15
docs:
  - docs/mailbox/kernel/inbox/2026-04-15_078_audit-chain-prelaunch-cleanup.md
  - docs/mailbox/infra/outbox/2026-04-15_089_audit-chain-genesis-hash-done.md
---

## Tartalom

Probe/E2E teszt tenantok audit eventjeinek törlése production DB-ből.
Doorstar éles tenant érintetlen marad.
Genesis hash már stabil (INFRA-089) — ez csak cleanup.
