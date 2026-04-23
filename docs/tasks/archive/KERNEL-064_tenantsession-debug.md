---
id: KERNEL-064
title: TenantSessionInterceptor GUID mismatch debug + fix
status: active
priority: critical
assignee: kernel
epic: claimsresolver-fix
blocked_by: ~
created: 2026-04-13
updated: 2026-04-13
docs:
  - docs/mailbox/kernel/inbox/2026-04-13_064_tenancysession-debug.md
---

VPS journal alapú debug: miért 500 a POST flow-epics/workstations/space-layers?
Root cause: TenantSessionInterceptor GUID normalizálás (8dd0bd7) → mismatch a DB-ben tárolt
TenantId és a session filter értéke között.
Párhuzamosan: INFRA-066 rollback c62f1d7-re.
