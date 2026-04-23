---
id: INFRA-075
title: Keycloak Script Mapper — tid flat claim hozzáadása
status: active
priority: critical
assignee: VPS Operator (INFRA)
epic: BATCH-0-CLEANUP
blocked_by:
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/infra/inbox/2026-04-14_075_keycloak-tid-claim-mapper.md
  - docs/mailbox/e2e/outbox/2026-04-14_017_05close-jwt-diagnosis-done.md
---

## Feladat

Keycloak spaceos-tenant-mapper script bővítése: `tid = spaceos_tenants[0].tenant_id` flat claim.

**Root cause:** tid hiánya → ClaimsTenantResolver null fallback → RLS mismatch → PUT /close 500.

**Blokkolja:** 05-close FSM E2E fail + BATCH-0-CLEANUP-01.

## Döntés indoka (root)

INFRA-oldali fix biztonságosabb mint ClaimsTenantResolver újabb módosítása (MSG-066 revert precedens).
Architecturálisan helyes: `tid` a kanonikus tenant ID claim — mindkét resolver ebből olvas.
