# INFRA Terminál

> VPS infrastruktúra — nginx, systemd, SSL, PostgreSQL, Keycloak

## Gyors Info

| | |
|---|---|
| **Terminál** | infra |
| **Port** | - |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/infra/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/infra/` |
| **Memory** | `/opt/spaceos/docs/memory/infra.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/infra.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/infra/inbox/

# 3. Service státuszok
sudo systemctl status nginx
sudo systemctl status postgresql
```

## Gyakori Parancsok

```bash
# Nginx
sudo nginx -t                           # Config teszt
sudo systemctl reload nginx             # Reload
sudo systemctl restart nginx            # Restart
tail -f /var/log/nginx/error.log        # Error log

# PostgreSQL
sudo systemctl status postgresql
sudo -u postgres psql                   # psql shell
sudo -u postgres pg_dump spaceos > backup.sql

# SSL (Let's Encrypt)
sudo certbot certificates               # Cert lista
sudo certbot renew                      # Renewal
sudo certbot --expand -d new.domain.hu  # Új domain

# Systemd
sudo systemctl status spaceos-*         # SpaceOS szolgáltatások
sudo journalctl -u spaceos-knowledge -f # Log követés
```

## Nginx Konfigurációk

```
/etc/nginx/sites-enabled/
├── default                  ← Alap frontend
├── nexus-knowledge          ← MCP Knowledge Service
├── keycloak                 ← Keycloak proxy
└── api                      ← Backend proxy
```

## Systemd Szolgáltatások

| Service | Port | Leírás |
|---------|------|--------|
| `spaceos-knowledge` | 3456 | MCP Knowledge Service |
| `nginx` | 80/443 | Reverse proxy |
| `postgresql` | 5432 | Database |

## DONE Outbox Sablon

```yaml
---
id: MSG-INFRA-NNN-DONE
from: infra
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-INFRA-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit konfiguráltam/deployoltam.

## Érintett szolgáltatások
Mely systemd szolgáltatások változtak.

## Tesztelés
curl/healthcheck eredmények.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/infra/CLAUDE.md`
- Deployment Runbook: `/opt/spaceos/docs/knowledge/deployment/DEPLOYMENT_RUNBOOK.md`
- Known Gotchas: `/opt/spaceos/docs/knowledge/deployment/KNOWN_GOTCHAS.md`
