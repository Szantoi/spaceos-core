# JOINERY Terminál

> .NET 8 Joinery modul — ajtógyártási domain logika

## Gyors Info

| | |
|---|---|
| **Terminál** | joinery |
| **Port** | 5002 |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/backend/spaceos-modules-joinery/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/joinery/` |
| **Memory** | `/opt/spaceos/docs/memory/joinery.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/joinery.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/joinery/inbox/

# 3. Build és teszt
cd /opt/spaceos/backend/spaceos-modules-joinery
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
dotnet run --project src/SpaceOS.Modules.Joinery.Api

# Health check
curl http://localhost:5002/healthz
```

## Architektúra

```
src/
├── SpaceOS.Modules.Joinery.Api/           ← ASP.NET Core API
├── SpaceOS.Modules.Joinery.Domain/        ← Domain entities
├── SpaceOS.Modules.Joinery.Application/   ← Use cases
├── SpaceOS.Modules.Joinery.Persistence/   ← EF Core
└── SpaceOS.Modules.Joinery.Contracts/     ← NuGet package
```

## Domain Entitások

- **DoorConfiguration** — ajtó konfiguráció (méretek, típus, felület)
- **FrameType** — tok típusok (fa, acél, alumínium)
- **LeafType** — ajtólap típusok
- **HardwareSet** — vasalat készletek

## Provider Interfész

A Joinery modulnak implementálnia kell az `IParametricProduct` interfészt, amit a Kernel definiál.

## DONE Outbox Sablon

```yaml
---
id: MSG-JOINERY-NNN-DONE
from: joinery
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-JOINERY-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit implementáltam, commit hash.

## Tesztek
dotnet test eredmény.

## API Endpoints
Új/módosított endpointok listája.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/backend/spaceos-modules-joinery/CLAUDE.md`
- Knowledge: `/opt/spaceos/docs/knowledge/context/JOINERY_CONTEXT.md`
