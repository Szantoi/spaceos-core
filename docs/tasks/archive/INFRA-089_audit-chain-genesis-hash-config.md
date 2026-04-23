---
id: INFRA-089
title: AuditChain:GenesisHash production konfig fix
status: active
priority: high
assignee: infra
epic: AUDIT-INTEGRITY
blocked_by:
created: 2026-04-15
updated: 2026-04-15
docs:
  - docs/mailbox/infra/inbox/2026-04-15_089_audit-chain-genesis-hash-config.md
  - docs/mailbox/kernel/outbox/2026-04-15_077_audit-chain-investigation-done.md
---

## Tartalom

`appsettings.Production.json`-ba `AuditChain:GenesisHash` stabil hex értéket beállítani,
hogy minden restart után ne generáljon új random genesis hash-t a kernel.
