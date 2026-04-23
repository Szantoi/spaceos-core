---
id: E2E-043
title: E2E rerun — nesting aktiválás (DB grant után)
status: archive
priority: high
assignee: e2e
epic: sprint-8-q3
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/e2e/inbox/2026-04-16_043_rerun-nesting-activation.md
  - docs/mailbox/e2e/outbox/2026-04-16_043_rerun-nesting-activation-done.md
---

214/214 ✅. 42501 megszűnt (INFRA-112 OK). Nesting skip: SqlState 42704 — unrecognized
configuration parameter "app.current_tenant_id" — a spaceos_cutting/inventory/procurement
DB-ken nincs regisztrálva ez a custom GUC. INFRA-113 kiadva a fix-hez.
