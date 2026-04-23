---
id: CUTTING-008
title: TenantSessionInterceptor — DbConnectionInterceptor mindhárom modulban
status: archive
priority: high
assignee: cutting
epic: sprint-8-q3
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/cutting/inbox/2026-04-16_008_tenant-session-interceptor.md
  - docs/mailbox/cutting/outbox/2026-04-16_008_tenant-session-interceptor-done.md
---

TenantSessionInterceptor (DbConnectionInterceptor): tid claim → set_config('app.current_tenant_id').
ConnectionClosingAsync: reset üres string-re. AddHttpContextAccessor + Singleton regisztráció.
Commits: inventory a363ad6 · procurement 0dbb02e · cutting 1ae66a0. 153/153 ✅.
Security: paraméteres set_config, pool cleanup. INFRA-114 deploy kiadva.
