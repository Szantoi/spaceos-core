# SALES Terminál

> .NET 8 Sales modul — értékesítés, árajánlat, megrendelés

## Gyors Info

| | |
|---|---|
| **Terminál** | sales |
| **Port** | 5007 |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/backend/spaceos-modules-sales/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/sales/` |
| **Memory** | `/opt/spaceos/docs/memory/sales.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/sales.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/sales/inbox/

# 3. Build és teszt
cd /opt/spaceos/backend/spaceos-modules-sales
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
dotnet run --project src/SpaceOS.Modules.Sales.Api

# Health check
curl http://localhost:5007/healthz
```

## Domain Entitások

- **Quote** — árajánlat
- **QuoteLine** — ajánlati tétel
- **Order** — megrendelés
- **OrderLine** — rendelési tétel
- **Customer** — ügyfél
- **PricingRule** — árszabály

## DONE Outbox Sablon

```yaml
---
id: MSG-SALES-NNN-DONE
from: sales
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-SALES-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit implementáltam, commit hash.

## Tesztek
dotnet test eredmény.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/backend/spaceos-modules-sales/CLAUDE.md`
