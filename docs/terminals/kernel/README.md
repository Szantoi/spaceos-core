# KERNEL Terminál

> .NET 8 backend core - auth, audit, FSM, tenant management

## Gyors Info

| | |
|---|---|
| **Terminál** | kernel |
| **Port** | 5000 |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/backend/spaceos-kernel/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/kernel/` |
| **Memory** | `/opt/spaceos/docs/memory/kernel.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/kernel.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/kernel/inbox/

# 3. Build és teszt
cd /opt/spaceos/backend/spaceos-kernel
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
dotnet run --project src/SpaceOS.Kernel.Api

# Health check
curl http://localhost:5000/healthz
```

## Fontos API Endpointok

```
GET  /healthz                              Health check
GET  /api/tenants                          Tenant lista
POST /api/tenants                          Új tenant
GET  /api/tenants/{id}/facilities          Facility-k
POST /api/facilities/{id}/flow-epics       Új FlowEpic

DELETE /internal/flow-epics/by-tenant      Internal cleanup (4-gate security)
```

## Architektúra

```
src/
├── SpaceOS.Kernel.Api/           ← ASP.NET Core API
├── SpaceOS.Kernel.Domain/        ← Domain entities, events
├── SpaceOS.Kernel.Application/   ← Use cases, handlers
├── SpaceOS.Kernel.Persistence/   ← EF Core, migrations
└── SpaceOS.Kernel.Contracts/     ← NuGet package, shared DTOs
```

## Fontos Szabályok

1. **RLS nem GUC-alapú** - `IgnoreQueryFilters()` + explicit WHERE
2. **Immutability** - nincs UPDATE CAD adatokon
3. **Audit trail** - minden művelet SHA-256 hashed event
4. **FlowEpic sorrend** - Tenant → Facility → FlowEpic (nincs közvetlen POST)

## DONE Outbox Sablon

```yaml
---
id: MSG-KERNEL-NNN-DONE
from: kernel
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-KERNEL-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit implementáltam, commit hash.

## Tesztek
Hány teszt, mind zöld.

## Security review
Input validation, auth, RLS ellenőrizve.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/backend/spaceos-kernel/CLAUDE.md`
- Knowledge: `/opt/spaceos/docs/knowledge/context/KERNEL_CONTEXT.md`
- Patterns: `/opt/spaceos/docs/knowledge/patterns/DATABASE_PATTERNS.md`
