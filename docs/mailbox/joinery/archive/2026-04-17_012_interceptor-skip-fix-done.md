---
id: MSG-JOINERY-012-DONE
from: joinery
to: root
type: response
priority: high
status: READ
ref: MSG-JOINERY-012
created: 2026-04-17
---

# MSG-JOINERY-012-DONE — TenantSessionInterceptor skip fix + JoineryOutboxEntries

## Összefoglaló

### 1. TenantSessionInterceptor — GUC overwrite fix

**Root cause:** `ConnectionOpenedAsync` mindig meghívta `SetConfigAsync`-t, akkor is ha `ResolveTenantId()` null-t adott vissza. Az `?? string.Empty` miatt üres stringgel hívta, ami felülírta a handler-ben beállított session-level GUC-ot.

**Fix (`TenantSessionInterceptor.cs`):**
```csharp
var tenantId = ResolveTenantId();
if (string.IsNullOrWhiteSpace(tenantId))
{
    await base.ConnectionOpenedAsync(connection, eventData, ct).ConfigureAwait(false);
    return;
}
await SetConfigAsync(connection, PgConfigKey, tenantId, ct).ConfigureAwait(false);
await base.ConnectionOpenedAsync(connection, eventData, ct).ConfigureAwait(false);
```

Belső hívásokra az interceptor skippeli a set_config-ot — az `InternalEndpoints.cs` handler-szintű értéke megmarad.

### 2. JoineryOutboxEntries — kód vizsgálat eredménye

- Migration: **megvan** — `20260410000001_J0002_V2_CuttingListSnapshot.cs` tartalmazza a CREATE TABLE-t
- `MigrateAsync()`: **megvan** — `Program.cs` 72-79. sor, `IsRelational()` guard-dal
- `DeleteAllByTenantAsync`: **már törli** az OutboxEntries-eket (89-94. sor)

Kód oldalon nincs tennivaló. A `42P01` hiba régi VPS deploy-ból ered — friss deploy után a `MigrateAsync()` automatikusan létrehozza a táblát.

## Build & Test

```
dotnet build  → 0 error, 0 warning
dotnet test   → 219/219 passed
```

Commit: `c8ac5b6 fix(joinery): skip GUC set in interceptor when no JWT (JOINERY-012)`

## Security review

- GUC-set skip csak belső hívásokra (no JWT) → RLS bypass kizárt: az interceptor nem ad GUC értéket, a handler explicit set_config-ja érvényes
- `ConnectionClosingAsync` reset (`string.Empty`) megmarad — connection pool cleanup helyes
- Nincs új endpoint, nincs új input surface
