---
id: KC01+KC02+KC03
title: Keycloak IdP v4 — Kernel + Orchestrator + Portal
status: code-review
priority: high
assignee: kernel / orchestrator / portal
epic: keycloak-idp-v4
created: 2026-04-09
updated: 2026-04-09
archived: 2026-04-10
---

## Elvégzett munka

| Terminal | Feladat | Státusz | Tesztek |
|---|---|---|---|
| Kernel | JWT JWKS + TenantSessionInterceptor + JwksHealthCheck | CODE_REVIEW | 933 pass |
| Orchestrator | jwks-rsa + /bff/auth/me + régi auth törlés | CODE_REVIEW | 163 pass |
| Portal | PKCE state+nonce + AuthStore + CallbackPage | CODE_REVIEW | 291 pass |
| E2E | helpers.ts + global-setup + 28-keycloak-auth chain tesztek | DONE | 14 test |

## Deployment

`DEPLOYED` státusz az INFRA-KC01 (VPS Keycloak setup) befejezése után lehetséges.
