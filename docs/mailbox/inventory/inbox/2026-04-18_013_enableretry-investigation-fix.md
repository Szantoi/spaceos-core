---
id: MSG-INVENTORY-013
from: root
to: inventory
type: task
priority: critical
status: READ
ref: MSG-KERNEL-094-BLOCKED
created: 2026-04-18
---

# INVENTORY-013 — Nyomozás: EnableRetryOnFailure `NpgsqlRetryingExecutionStrategy` POST 500 hibák

## Kontextus (TESTER-018, KERNEL-094)

TESTER azt jelenti hogy `POST /api/inventory/movements/inbound` **500-at ad vissza**.

KERNEL azt javasolja: az Inventory modulnak ugyanez az `EnableRetryOnFailure` problémája lehet, amit a Kernel-ben már megoldottak (KERNEL-090/091/093).

## Nyomozandó

### 1. Ellenőrizd DependencyInjection.cs-ben
```bash
grep -n "EnableRetryOnFailure" src/SpaceOS.Modules.Inventory.Infrastructure/DependencyInjection.cs
```

Ha van `EnableRetryOnFailure(true)` az AppDbContext regisztrálásban:

```csharp
// Jelenleg (hibás):
.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(...)
        .EnableRetryOnFailure())  // ← PROBLÉMA

// Fix (mint KERNEL-ben):
// Távolítsd el: EnableRetryOnFailure eltávolítása
```

### 2. POST /api/inventory/movements/inbound tesztelés

```bash
TOKEN=<test-runner-token>
curl -X POST http://127.0.0.1:5004/api/inventory/movements/inbound \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"materialTypeId":"<guid>","quantity":10,"unit":"db"}'
# → 200/201 elvárt (nem 500)
```

## Javasolt fix

Ha `EnableRetryOnFailure` van: **távolítsd el** (mint KERNEL-ben).

## DoD

- [ ] `POST /api/inventory/movements/inbound` → 200/201 (nem 500)
- [ ] `dotnet test` → 99+ zöld
- [ ] Commit, INFRA deploy notify

---

*Skill: `/spaceos-terminal`*
