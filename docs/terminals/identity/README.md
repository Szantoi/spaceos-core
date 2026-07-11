# IDENTITY Terminál

> .NET 8 Identity modul — felhasználókezelés, jogosultságok

## Gyors Info

| | |
|---|---|
| **Terminál** | identity |
| **Port** | 5008 |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/backend/spaceos-modules-identity/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/identity/` |
| **Memory** | `/opt/spaceos/docs/memory/identity.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/identity.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/identity/inbox/

# 3. Build és teszt
cd /opt/spaceos/backend/spaceos-modules-identity
dotnet build
dotnet test
```

## Build & Test Parancsok

```bash
# Build
dotnet build

# Unit tesztek
dotnet test

# Futtatás (dev)
dotnet run --project src/SpaceOS.Modules.Identity.Api

# Health check
curl http://localhost:5008/healthz
```

## Domain Entitások

- **User** — felhasználó
- **Role** — szerepkör
- **Permission** — jogosultság
- **Team** — csapat
- **Invitation** — meghívó

## Keycloak Integráció

Az Identity modul Keycloak-kal integrálódik:
- JWT token validáció
- Role mapping
- User provisioning

## DONE Outbox Sablon

```yaml
---
id: MSG-IDENTITY-NNN-DONE
from: identity
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-IDENTITY-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit implementáltam, commit hash.

## Security review
RBAC, JWT validáció ellenőrizve.

## Tesztek
dotnet test eredmény.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/backend/spaceos-modules-identity/CLAUDE.md`
- Security Patterns: `/opt/spaceos/docs/knowledge/security/SECURITY_PATTERNS.md`
