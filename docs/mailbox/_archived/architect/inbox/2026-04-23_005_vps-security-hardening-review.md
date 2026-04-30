---
id: MSG-ARCH-005
from: root
to: architect
type: task
priority: critical
status: READ
created: 2026-04-23
---

# ARCH-005 — VPS Security Hardening Review + INFRA task spec

> **Kontextus:** Lynis audit lefutott, Hardening Index 65/100. Root előelemzést végzett (lásd alább).
> A te feladatod: validáld a fix tervet, azonosíts kockázatokat, és írj egy részletes INFRA task spec-et amit az INFRA terminál végre tud hajtani.
> **Output:** `docs/mailbox/architect/outbox/` response + opcionálisan `docs/architecture/` security doc

## Audit eredmény összefoglaló

**Lynis Hardening Index: 65/100**
**Audit riport:** `/tmp/vps-audit-2026-04-23.md`
**Lynis log:** `/tmp/lynis-log.txt`

## Azonosított kritikus kockázatok

### 3 nyitott port (PUBLIC bind)

| Port | Service | Jelenlegi bind | Probléma |
|---|---|---|---|
| 5432 | PostgreSQL (Docker) | `0.0.0.0:5432` | DB közvetlenül elérhető az internetről |
| 5050 | pgAdmin (Docker) | `0.0.0.0:5050` | Admin UI elérhető az internetről |
| 8080 | Keycloak | `*:8080` | Auth szerver elérhető nginx nélkül |

### 23 outdated security package

Kritikusak: `openssh-server`, `openssl`, `libssl3t64`, `bind9-libs`

### SSH konfig gyengeségek

- `PermitRootLogin yes`
- `MaxAuthTries 6` (default)
- `MaxSessions 10` (default)
- Nincs `AllowUsers` korlátozás

### Redis

- `CONFIG` command nem tiltott (rename-command hiányzik)

### PostgreSQL

- 3 config fájl world-readable (`/etc/postgresql/17/main/`)

## Root előzetes prioritizálása

**Batch 1 (KRITIKUS, ~30 perc):** S1-S5 — port binding fix, apt upgrade, SSH root login
**Batch 2 (HIGH, ~40 perc):** S6-S10 — PG chmod, SSH hardening, Redis, unknown port
**Batch 3 (MEDIUM, ~1.5 óra):** S11-S17 — auditd, AIDE, rkhunter, protocol disable

## Amit az Architect-től várok

1. **Validáld a prioritizálást** — helyes a sorrend? Kimaradt valami kritikus?
2. **Docker port fix stratégia** — a `docker-compose.yml` bind módosítás elég, vagy UFW Docker chain is kell? (Docker ismerten megkerüli az UFW-t)
3. **Keycloak bind** — `KC_HTTP_HOST=127.0.0.1` elég, vagy a systemd unit-ban is kell valami?
4. **SSH hardening** — melyik beállítások biztonságosak anélkül hogy kizárnánk magunkat? (Gábor VS Code Remote-on dolgozik, több session kell)
5. **Backward compatibility** — a fix-ek nem törhetik el a futó service-eket (Kernel, Orchestrator, Joinery, FreeTier, stb.)
6. **Írj részletes INFRA task spec-et** amit az INFRA terminál lépésről lépésre végre tud hajtani — rollback parancsokkal minden lépésnél
7. **Bedrock MC szerver** — `0.0.0.0:19132/udp` nyitott. Kockázat?

## Definition of Done

- [ ] Prioritizálás validálva / javítva
- [ ] Docker + UFW interakció elemezve
- [ ] INFRA task spec kész (batch 1 minimum, rollback parancsokkal)
- [ ] Backward compatibility ellenőrizve
- [ ] Outbox response küldve
