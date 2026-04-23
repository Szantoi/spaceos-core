---
id: MSG-INFRA-KC01
title: Keycloak VPS Production Setup
status: active
priority: high
assignee: VPS Operator
epic: keycloak-idp-v4
blocked_by: VPS Operator (manuális lépések)
created: 2026-04-09
updated: 2026-04-10 09:xx
ref: docs/mailbox/infra/inbox/2026-04-09_049_keycloak-vps-setup.md
---

## Haladás (2026-04-10)

| Lépés | Státusz |
|---|---|
| PostgreSQL DB izoláció (DB-05) | ✅ KÉSZ |
| Keycloak Docker indítás | ✅ KÉSZ |
| Realm setup (spaceos) | 🔄 FOLYAMATBAN |
| realm-export.json exportálás | ⬜ TODO |
| Nginx /auth/ proxy + CSP | ⬜ TODO |
| E2E env vars beállítás | ⬜ TODO |
| Változásnapló dokumentum | ✅ KÉSZ |

## Blokkol

- KC01 / KC02 / KC03 → `DEPLOYED` státusz
- E2E live run valódi Keycloak-kal

## DoD

Amikor kész: outbox üzenet érkezik, státusz → `archive/`
