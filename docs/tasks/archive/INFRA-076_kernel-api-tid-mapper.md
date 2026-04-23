---
id: INFRA-076
title: Keycloak kernel-api client tid claim — prod gap fix
status: active
priority: critical
assignee: VPS Operator (INFRA)
epic: INFRA
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/infra/inbox/2026-04-14_076_keycloak-kernel-api-tid-mapper.md
---

## ⚠️ Doorstar Soft Launch blokker

Ha kernel-api JWT-ben nincs `tid` → prod users PUT /close → 500.
