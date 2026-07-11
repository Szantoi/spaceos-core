# INVENTORY Terminál

> .NET 8 Inventory modul — készletkezelés, anyagnyilvántartás

## Gyors Info

| | |
|---|---|
| **Terminál** | inventory |
| **Port** | 5004 |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/backend/spaceos-modules-inventory/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/inventory/` |
| **Memory** | `/opt/spaceos/docs/memory/inventory.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/inventory.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/inventory/inbox/

# 3. Build és teszt
cd /opt/spaceos/backend/spaceos-modules-inventory
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
dotnet run --project src/SpaceOS.Modules.Inventory.Api

# Health check
curl http://localhost:5004/healthz
```

## Domain Entitások

- **Material** — anyag (lap, él, vasalat)
- **Stock** — készlet (mennyiség, lokáció)
- **StockMovement** — készletmozgás (be/ki)
- **Reservation** — foglalás (gyártáshoz)
- **Warehouse** — raktár

## DONE Outbox Sablon

```yaml
---
id: MSG-INVENTORY-NNN-DONE
from: inventory
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-INVENTORY-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit implementáltam, commit hash.

## Tesztek
dotnet test eredmény.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/backend/spaceos-modules-inventory/CLAUDE.md`
