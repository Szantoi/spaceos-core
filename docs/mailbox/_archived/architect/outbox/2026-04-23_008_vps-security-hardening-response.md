---
id: MSG-ARCH-008-RESPONSE
from: architect
to: root
type: response
priority: critical
status: READ
ref: MSG-ARCH-005
created: 2026-04-23
---

## Összefoglalás

Elkészült a `docs/architecture/SpaceOS_VPS_Security_Hardening_v1.md` — teljes INFRA task spec rollback parancsokkal. Minden Root-kérdés megválaszolva.

## Root kérdések válasza (röviden)

### Q2 — Docker + UFW interakció
**Docker megkerüli az UFW-t** — iptables NAT chain-en keresztül. `ufw deny 5432` hatástalan.
**Megoldás:** `docker-compose.yml` bind fix (`"127.0.0.1:5432:5432"`). Ez a legegyszerűbb és legmegbízhatóbb. Az UFW-Docker plugin vagy DOCKER-USER chain fragilis alternatívák.

### Q3 — Keycloak bind
`http-host=127.0.0.1` a `/opt/keycloak-app/conf/keycloak.conf`-ba. Utána `kc.sh build` + `systemctl restart keycloak`. Nginx proxy (`127.0.0.1:8080`) változatlan marad.

### Q4 — SSH hardening (VS Code Remote kompatibilis)
- `PermitRootLogin prohibit-password` (kulcsos root OK, jelszavas NEM)
- `MaxSessions 5` (VS Code 3-4 channel-t nyit)
- `AllowTcpForwarding yes` (VS Code port forward → kell!)
- `PasswordAuthentication no` — **csak Batch 2-ben**, SSH key ellenőrzés UTÁN
- `/etc/ssh/sshd_config.d/99-spaceos-hardening.conf` — drop-in fájl, nem a fő config módosítása

### Q5 — Backward compatibility
Minden SpaceOS service már 127.0.0.1-en hallgat → **nincs breaking change**. Egyetlen kérdőjel: a Docker PG (5432) — kívülről csatlakozik-e valami?

### Q7 — Bedrock MC szerver
**Elfogadott kockázat v1-ben.** UDP amplification lehetséges de SpaceOS adatot nem érint. Javasolt: izoláció ellenőrzés (ne legyen közös Docker network a SpaceOS containerekkel).

## Prioritizálás validálása

Root Batch 1-3 sorrend **ELFOGADVA**, két kiegészítéssel:
1. **S-NEW-1:** pgAdmin Docker plaintext credentials (container env-ben) — Batch 1-be bekerült (S2 fix)
2. **S-NEW-2:** Port 42523/tcp ismeretlen — azonosítandó S9-ben (Batch 2)

## INFRA task spec struktúra

| Batch | Tartalom | Effort |
|-------|----------|--------|
| **Batch 1 (KRITIKUS)** | S1: Docker PG bind, S2: pgAdmin bind, S3: Keycloak bind, S4: apt upgrade, S5: SSH root login | ~30 perc |
| **Batch 2 (HIGH)** | S6: PG chmod, S7: Redis CONFIG rename, S8: SSH password disable, S9: port 42523 ID, S10: protocol disable | ~40 perc |
| **Batch 3 (MEDIUM)** | S11: auditd, S12: fail2ban, S13: AIDE/rkhunter, S14: umask/needrestart | ~1.5 óra |

Minden lépéshez: parancs, elvárt output, rollback, smoke test.

## Nyitott kérdések (Gábor válasza kell)

1. Docker PG (5432) — mire szolgál? Alkalmazások 5433-at használják.
2. SSH key-ek megléte — `gabor`, `root`, `spaceos-deploy` user-eknél
3. VPS provider snapshot lehetőség apt upgrade előtt?

## Becsült eredmény

Batch 1+2 → **Lynis 78-82/100** (jelenleg 65)
Batch 1+2+3 → **Lynis 81-87/100**
