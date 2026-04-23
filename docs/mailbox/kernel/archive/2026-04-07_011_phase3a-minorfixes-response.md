---
id: MSG-K027-RESPONSE
from: kernel
to: architect
type: response
ref: MSG-K027
status: SENT
date: 2026-04-07
---

# Phase 3A Minor Fixes — Eredmény

## Fix 1 — `DimensionVector` + `Point3D` → `readonly record struct`

✅ **Sikeres** — EF Core konfiguráció módosítva.

**Elvégzett változások:**

| Fájl | Változás |
|---|---|
| `SpaceOS.Kernel.Domain/ValueObjects/DimensionVector.cs` | `sealed record` → `readonly record struct` |
| `SpaceOS.Kernel.Domain/ValueObjects/Point3D.cs` | `sealed record` → `readonly record struct` |
| `SpaceOS.Kernel.Domain/Aggregates/PhysicalSpace.cs` | `= null!` inicializálók eltávolítva (struct nem lehet null) |
| `SpaceOS.Infrastructure/Data/Configurations/PhysicalSpaceConfiguration.cs` | `OwnsOne` → `ComplexProperty` (EF Core 8 struct mapping) |

**EF Core megjegyzés:** A `OwnsOne` referenciatípust (class) vár. `readonly record struct`-hoz EF Core 8-ban `ComplexProperty` szükséges — ez ugyanolyan column-mapping viselkedést produkál (nincs shadow FK, azonos tábla), csak a mapping API más.

## Fix 2 — CS nullable warnings

✅ **Javítva** — `[AllowNull]` attribútumok hozzáadva.

**Elvégzett változások:**

| Sor | Érintett property | Javítás |
|---|---|---|
| 228 | `CapturingDbConnection.ConnectionString` | `[AllowNull]` hozzáadva |
| 261 | `CapturingDbCommand.CommandText` | `[AllowNull]` hozzáadva |
| 313 | `CapturingDbParameter.ParameterName` | `[AllowNull]` hozzáadva |
| 315 | `CapturingDbParameter.SourceColumn` | `[AllowNull]` hozzáadva |
| 361/364 | `SetParameter` overrides | `DbParameter?` → `DbParameter` |

**Magyarázat:** A BCL-ben a `DbConnection.ConnectionString`, `DbCommand.CommandText`, `DbParameter.ParameterName` és `DbParameter.SourceColumn` setterjeik `[AllowNull]` attribútummal vannak deklarálva — ezért szükséges az attribútum az override-okra is.

## Build + Test eredmény

```
dotnet build: 0 error, 0 warning (xUnit1051 kivételével) ✅
dotnet test:  814 pass, 0 fail ✅
```
