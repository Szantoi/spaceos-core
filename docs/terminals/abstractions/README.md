# ABSTRACTIONS Terminál

> .NET 8 Abstractions modul — közös interfészek, base osztályok

## Gyors Info

| | |
|---|---|
| **Terminál** | abstractions |
| **Port** | 5003 |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/backend/spaceos-modules-abstractions/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/abstractions/` |
| **Memory** | `/opt/spaceos/docs/memory/abstractions.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/abstractions.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/abstractions/inbox/

# 3. Build és teszt
cd /opt/spaceos/backend/spaceos-modules-abstractions
dotnet build
dotnet test
```

## Build & Test Parancsok

```bash
# Build
dotnet build

# Unit tesztek
dotnet test

# NuGet pack (ha van)
dotnet pack
```

## Fő Komponensek

- **IParametricProduct** — parametrikus termék interfész
- **Base Entity osztályok** — AuditableEntity, TenantEntity
- **Value Objects** — Dimension, Money, Quantity
- **Domain Events** — base event osztályok

## Használat Más Modulokban

Az Abstractions NuGet csomagként kerül referálásra a többi modulban:

```xml
<PackageReference Include="SpaceOS.Modules.Abstractions" Version="1.0.0" />
```

## DONE Outbox Sablon

```yaml
---
id: MSG-ABSTRACTIONS-NNN-DONE
from: abstractions
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-ABSTRACTIONS-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit implementáltam, commit hash.

## Breaking Changes
Ha van, explicit listázni.

## Tesztek
dotnet test eredmény.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/backend/spaceos-modules-abstractions/CLAUDE.md`
- Module Boundaries: `/opt/spaceos/docs/knowledge/architecture/MODULE_BOUNDARIES.md`
