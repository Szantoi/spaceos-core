---
id: INFRA-087
title: Kernel 4cafceb + Migration 0030 VPS deploy
status: active
priority: high
assignee: infra
epic: SPRINT-6
blocked_by:
created: 2026-04-15
updated: 2026-04-15
docs:
  - docs/mailbox/infra/inbox/2026-04-15_087_kernel-migration0030-deploy.md
---

## Tartalom

- Kernel develop frissítés (KERNEL-075 commit tartalmazza az AuditEvents.Sequence column-t)
- Migration 0030 (`AddAuditEventSequence`) futtatása VPS PostgreSQL-en
- systemd restart + healthz ellenőrzés
