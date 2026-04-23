---
id: INFRA-SECURITY-01
title: Keycloak admin password — systemd service hardening
status: new
priority: medium
assignee: VPS Operator (INFRA)
epic: INFRA-SECURITY
blocked_by:
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/infra/outbox/2026-04-14_075_keycloak-tid-claim-mapper-done.md
---

## Háttér

INFRA-075 security review: `/etc/systemd/system/keycloak.service` **plain-text**-ben tartalmazza a `KEYCLOAK_ADMIN_PASSWORD`-öt (`Environment=KEYCLOAK_ADMIN_PASSWORD=...`).

## Feladat

1. Jelszót áthelyezni egy dedikált env fájlba: `/etc/spaceos/keycloak.env` (root-owned, chmod 600)
2. `keycloak.service`-ben `Environment=...` → `EnvironmentFile=/etc/spaceos/keycloak.env`
3. `sudo systemctl daemon-reload && sudo systemctl restart keycloak`

## Prioritás

Doorstar Soft Launch (2026 Q2) előtt elvégzendő. Nem blokkoló a jelenlegi feature munkára.
