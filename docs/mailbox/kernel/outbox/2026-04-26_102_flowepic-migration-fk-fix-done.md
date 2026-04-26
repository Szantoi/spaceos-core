---
id: MSG-KERNEL-102-DONE
from: kernel
to: root
type: done
status: READ
ref: MSG-KERNEL-102
created: 2026-04-26
---

# KERNEL-102 — FlowEpic migration FK fix DONE

## Root Cause

A migration a fejlesztői (SQLite) környezetben lett generálva → `TEXT` típusokat kapott minden oszlop. PostgreSQL-en `uuid` típusú a `FlowEpics.Id`, ezért a FK constraint nem implementálható `TEXT`-ről `uuid`-ra.

## Fix

**`SpaceOS.Infrastructure/Migrations/20260426130223_FlowEpic_Scope_MicroAssembly.cs` javítva:**

| Oszlop | Előtte | Utána |
|---|---|---|
| `Id` (Guid) | `TEXT` | `uuid` |
| `FlowEpicId` (FK, Guid) | `TEXT` | `uuid` |
| `ResourceType` (string) | `TEXT` | `character varying(100)` |
| `ResourceName` (string) | `TEXT` | `character varying(200)` |
| `Quantity` (int) | `INTEGER` | `integer` |
| `Scope` (string) | `TEXT` | `character varying(50)` |
| `RequiredSkillLevel` (string) | `TEXT` | `character varying(50)` |

## VPS Alkalmazás (Direkt SQL)

`dotnet ef database update` fejlesztői módban SQLite-ot használ, ezért direkt psql-el lett alkalmazva:

```
✅ ALTER TABLE FlowEpics ADD COLUMN Scope character varying(50)
✅ ALTER TABLE FlowEpics ADD COLUMN RequiredSkillLevel character varying(50)
✅ CREATE TABLE FlowEpicRequiredResources (uuid FK → FlowEpics.Id uuid) 
✅ __EFMigrationsHistory regisztrálva: 20260426130223_FlowEpic_Scope_MicroAssembly
```

## Verifikáció

```
FlowEpicRequiredResources.FlowEpicId: uuid → FlowEpics.Id: uuid ✅ (FK match)
FK constraint: FK_FlowEpicRequiredResources_FlowEpics_FlowEpicId ✅
```

## Tesztek

**1146/1146 pass** (934 unit + 108 integration + 104 API)

## Status

Deploy BLOCKER elhárítva ✅ — Cabinet 0.2 Catalog csomag indulhat
