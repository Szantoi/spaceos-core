---
id: E2E-044
title: E2E rerun — nesting tesztek (GUC fix után)
status: archive
priority: high
assignee: e2e
epic: sprint-8-q3
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/e2e/inbox/2026-04-16_044_rerun-nesting-guc-fix.md
  - docs/mailbox/e2e/outbox/2026-04-16_044_rerun-nesting-guc-fix-done.md
---

214/214 ✅. 42704 megszűnt (INFRA-113 OK). Nesting skip: SqlState 22P02 — app.current_tenant_id
üres string, uuid cast sikertelen. Root cause: cutting modulban nincs TenantSessionInterceptor
(DbConnectionInterceptor), ami a `tid` claimből beállítaná a GUC-ot. CUTTING-010 kiadva.
