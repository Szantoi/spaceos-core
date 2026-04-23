---
id: KERNEL-063
title: ClaimsTenantResolver graceful fallback (8dd0bd7 regresszió fix)
status: active
priority: critical
assignee: kernel
epic: e2e-stabilization
blocked_by: ""
created: 2026-04-13
updated: 2026-04-13
docs:
  - docs/mailbox/kernel/inbox/2026-04-13_063_claimsresolver-fallback.md
---

# KERNEL-063 — Fallback fix

spaceos_tenants hiányzik/malformed → esik vissza tid/groups claim-re, ne throw-oljon.
POST create 500 + GET list üres → javul.

## Mailbox

MSG-KERNEL-063 (kiadva: 2026-04-13)
