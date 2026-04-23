---
id: KERNEL-065
title: TenantSessionInterceptor visszaállítása c62f1d7-re
status: active
priority: critical
assignee: kernel
epic: claimsresolver-fix
blocked_by: ~
created: 2026-04-13
updated: 2026-04-13
docs:
  - docs/mailbox/kernel/inbox/2026-04-13_065_tenantsession-revert.md
---

git checkout c62f1d7 -- TenantSessionInterceptor.cs
ClaimsTenantResolver.cs megtartva (3645480 / tid-first + graceful fallback).
