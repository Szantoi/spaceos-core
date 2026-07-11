# PROCUREMENT Terminál

> .NET 8 Procurement modul — beszerzés, rendeléskezelés

## Gyors Info

| | |
|---|---|
| **Terminál** | procurement |
| **Port** | 5006 |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/backend/spaceos-modules-procurement/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/procurement/` |
| **Memory** | `/opt/spaceos/docs/memory/procurement.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/procurement.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/procurement/inbox/

# 3. Build és teszt
cd /opt/spaceos/backend/spaceos-modules-procurement
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
dotnet run --project src/SpaceOS.Modules.Procurement.Api

# Health check
curl http://localhost:5006/healthz
```

## Domain Entitások

- **Supplier** — beszállító
- **PurchaseOrder** — beszerzési rendelés
- **PurchaseOrderLine** — rendelési tétel
- **PriceList** — árlista
- **SupplierCatalog** — beszállítói katalógus

## DONE Outbox Sablon

```yaml
---
id: MSG-PROCUREMENT-NNN-DONE
from: procurement
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-PROCUREMENT-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit implementáltam, commit hash.

## Tesztek
dotnet test eredmény.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/backend/spaceos-modules-procurement/CLAUDE.md`
