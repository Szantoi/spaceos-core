# CUTTING Terminál

> .NET 8 Cutting modul — lapszabászati optimalizáció, nesting

## Gyors Info

| | |
|---|---|
| **Terminál** | cutting |
| **Port** | 5005 |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/backend/spaceos-modules-cutting/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/cutting/` |
| **Memory** | `/opt/spaceos/docs/memory/cutting.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/cutting.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/cutting/inbox/

# 3. Build és teszt
cd /opt/spaceos/backend/spaceos-modules-cutting
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
dotnet run --project src/SpaceOS.Modules.Cutting.Api

# Health check
curl http://localhost:5005/healthz
```

## Architektúra

```
src/
├── SpaceOS.Modules.Cutting.Api/           ← ASP.NET Core API
├── SpaceOS.Modules.Cutting.Domain/        ← Domain entities
├── SpaceOS.Modules.Cutting.Application/   ← Use cases, optimalizáció
├── SpaceOS.Modules.Cutting.Persistence/   ← EF Core
└── SpaceOS.Modules.Cutting.Contracts/     ← NuGet package
```

## Domain Entitások

- **CuttingOrder** — szabászati megrendelés
- **Panel** — alapanyag (lap)
- **Part** — kivágnivaló darab
- **NestingResult** — optimalizálási eredmény
- **CutPlan** — vágási terv (CNC-hez)

## Optimalizációs Algoritmusok

- **First Fit Decreasing** — egyszerű, gyors
- **Guillotine Cut** — egyenes vágások
- **Nesting Optimizer** — komplex 2D nesting

## DONE Outbox Sablon

```yaml
---
id: MSG-CUTTING-NNN-DONE
from: cutting
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-CUTTING-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit implementáltam, commit hash.

## Tesztek
dotnet test eredmény.

## Performance
Ha optimalizáció, benchmark eredmények.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/backend/spaceos-modules-cutting/CLAUDE.md`
- Knowledge: `/opt/spaceos/docs/knowledge/context/CUTTING_CONTEXT.md`
