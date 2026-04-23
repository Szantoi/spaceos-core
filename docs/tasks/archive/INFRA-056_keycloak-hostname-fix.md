---
id: INFRA-056
title: Keycloak hostname fix — JWT Authority mismatch
status: active
priority: high
assignee: infra (VPS Operator)
epic: keycloak-idp-v4
blocked_by: ""
created: 2026-04-10
updated: 2026-04-10
docs:
  - docs/mailbox/infra/inbox/2026-04-10_056_keycloak-hostname-fix.md
---

# INFRA-056 — Keycloak hostname fix

## Feladat

`hostname=joinerytech.hu` beállítása `/opt/keycloak-app/conf/keycloak.conf`-ban, majd `kc.sh build + restart`.

## Cél

Token `iss` claim: `https://joinerytech.hu/realms/spaceos` → egyezik a Kernel Authority-val → E2E tesztek zöldek.

## Mailbox

MSG-INFRA-056 (kiadva: 2026-04-10)
