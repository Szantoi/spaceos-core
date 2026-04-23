---
id: MSG-KERNEL-071
from: root
to: kernel
type: task
priority: high
status: READ
ref: MSG-INFRA-081-BLOCKED
created: 2026-04-15
---

# MSG-KERNEL-071 — Migration 0029 .Designer.cs regenerálás

## Root cause

A `20260415090000_Migration_0029_EcosystemActorTypes.cs` kézzel lett megírva, de a szükséges **`.Designer.cs`** és frissített **`AppDbContextModelSnapshot.cs`** fájlok hiányoznak. EF Core ezek nélkül nem fedezi fel a migrációt → `Database.MigrateAsync()` "no migrations to apply" → `TenantType` oszlop nem jön létre → `42703` runtime exception.

## Megoldás

### 1. Mentsd el a jelenlegi 0029 Up() tartalmát

A `20260415090000_Migration_0029_EcosystemActorTypes.cs` tartalmaz egyedi SQL-t (triggerek, constraint-ek). Mentsd el mielőtt törlöd:

```bash
cp SpaceOS.Infrastructure/Migrations/20260415090000_Migration_0029_EcosystemActorTypes.cs /tmp/migration_0029_backup.cs
```

### 2. EF migration remove

```bash
cd /opt/spaceos/SpaceOS.Kerner
dotnet ef migrations remove \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api
```

Ez törli a `20260415090000_*` fájlokat és visszaállítja a `ModelSnapshot.cs`-t a 0028 állapotra.

### 3. EF migration add — újragenerálás

```bash
dotnet ef migrations add Migration_0029_EcosystemActorTypes \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api
```

Ez létrehozza:
- `20260415090000_Migration_0029_EcosystemActorTypes.cs` (EF által generált Up/Down)
- `20260415090000_Migration_0029_EcosystemActorTypes.Designer.cs` ← **ez hiányzott**
- Frissített `AppDbContextModelSnapshot.cs` ← **ez is hiányzott**

### 4. Custom SQL visszamásolása

Az EF által generált `Up()` metódusba másold vissza a `/tmp/migration_0029_backup.cs` tartalmát — különösen:
- `TenantType` varchar(32) column + CHECK constraint
- `EnabledModules` JSONB column
- `TenantHandshakeAllowlist` tábla
- `prevent_tenant_type_change()` trigger (SEC-01)
- `validate_enabled_modules_for_type()` trigger (SEC-02)
- Demo seed adatok (ha vannak)

A `Down()` metódusban az inverz műveletek (DROP TRIGGER, DROP TABLE, DROP COLUMN).

### 5. Build + test

```bash
dotnet build
dotnet test --no-build
# Elvárás: ≥1104 pass, 0 fail
```

### 6. Commit + push

```bash
git add SpaceOS.Infrastructure/Migrations/
git commit -m "fix: regenerate migration 0029 Designer.cs and ModelSnapshot (MSG-071)"
git push origin develop
```

## DoD

- [ ] `20260415090000_Migration_0029_EcosystemActorTypes.Designer.cs` létezik
- [ ] `AppDbContextModelSnapshot.cs` tartalmazza a 0029 változásokat (TenantType, EnabledModules, TenantHandshakeAllowlist)
- [ ] `dotnet build` → 0 error
- [ ] ≥1104 teszt pass
- [ ] Commit + push develop
- [ ] DONE outbox: commit hash + teszt count
