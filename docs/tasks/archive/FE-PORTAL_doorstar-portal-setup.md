---
id: FE-PORTAL
title: Doorstar Portal FE — spaceos-fe session setup + scaffold
status: active
priority: high
assignee: infra + Gábor (Day 1), spaceos-fe (Day 2-3)
epic: doorstar-portal-v4.1
blocked_by: GitHub repo létrehozás (Gábor manuális)
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/tasks/new/SpaceOS_Doorstar_Portal_UI_Repo_Architecture_v4.1_Amendment.md
---

# Doorstar Portal FE Setup — v4.1 Implementation

## Státusz

| Feladat | Ki | Státusz |
|---|---|---|
| Dispatcher: `fe` terminal hozzáadás | Root (done) | ✅ KÉSZ |
| Mailbox `fe/inbox/` + `fe/outbox/` | Auto (dispatcher `ensure_dirs`) | ✅ AUTO |
| Keycloak portal-app localhost:5173 | INFRA-118 DONE ✅ | ✅ KÉSZ |
| GitHub: `spaceos-doorstar-portal` repo | Gábor ✅ | ✅ KÉSZ |
| VPS: repo clone `/opt/spaceos/spaceos-doorstar-portal/` | INFRA-119 DONE ✅ | ✅ KÉSZ |
| `sd --stop && sd --daemon` újraindítás | Gábor ✅ | ✅ KÉSZ |
| FE scaffold (MSG-FE-001) | 67d0cb4 ✅ | ✅ KÉSZ |
| Nginx + cert + KC (INFRA-120) | ⛔ BLOCKED: DNS hiány — Gábor feladata | ⛔ BLOCKED |
| Door Order dashboard (FE-002) | 4bc5984 ✅ 25 teszt | ✅ KÉSZ |
| Order completion flow (FE-003) | de696cc ✅ 40 teszt (+15) | ✅ KÉSZ |
| Nginx + cert (INFRA-123) | f19244f DEPLOYED ✅ SSL 2026-07-15 | ✅ KÉSZ |
| Új rendelés létrehozása (FE-004) | f19244f ✅ 55 teszt (+15) | ✅ KÉSZ |
| Rendelés státusz timeline + szűrés (FE-005) | 9237fc2 ✅ 72 teszt (+17) | ✅ KÉSZ |
| Error Boundary + 404 + Profil oldal (FE-006) | 9b6bd61 ✅ 87 teszt (+15) | ✅ KÉSZ |
| Contract Tests + Playwright scaffold (FE-007) | 3a0b931 ✅ 90 teszt (+3 contract) | ✅ KÉSZ |
| E2E auth flows: 01-login + 08-auth-edge + 09-responsive (FE-008) | folyamatban | 🔄 ACTIVE |

## Blokkolt

A FE scaffold csak akkor indulhat el, ha:
1. GitHub repo létezik: `github.com/Szantoi/spaceos-doorstar-portal`
2. VPS-en klónozva: `/opt/spaceos/spaceos-doorstar-portal/`
3. Dispatcher újraindítva (hogy `spaceos-fe` session létrejöhessen)

## Dispatcher változás

A `spaceos-dispatcher.sh` TERMINALS-ba felvéve:
```
[fe]="spaceos-fe:/opt/spaceos/spaceos-doorstar-portal"
```
→ Az `ensure_dirs` automatikusan létrehozza `docs/mailbox/fe/inbox/` és `outbox/` mappákat

## Megjegyzés — sd --launch-all

A v4.1 doc §3.2 és §6 `sd --launch-all`-t említ — ez **nem létezik**.
A helyes parancsok:
- `sd --launch` → csak root session indul (persistent)
- `spaceos-fe` on-demand: első inbox érkezésekor automatikusan indul
