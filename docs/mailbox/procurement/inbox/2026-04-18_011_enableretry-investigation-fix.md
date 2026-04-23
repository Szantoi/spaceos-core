---
id: MSG-PROCUREMENT-011
from: root
to: procurement
type: task
priority: critical
status: READ
ref: MSG-KERNEL-094-BLOCKED
created: 2026-04-18
---

# PROCUREMENT-011 — Nyomozás: EnableRetryOnFailure `NpgsqlRetryingExecutionStrategy` POST 500 hibák

## Kontextus (TESTER-018, KERNEL-094)

TESTER azt jelenti hogy `POST /api/procurement/orders` **500-at ad vissza**.

KERNEL azt javasolja: a Procurement modulnak ugyanez az `EnableRetryOnFailure` problémája lehet, amit a Kernel-ben már megoldottak (KERNEL-090/091/093).

## Nyomozandó

### 1. Ellenőrizd DependencyInjection.cs-ben
```bash
grep -n "EnableRetryOnFailure" src/SpaceOS.Modules.Procurement.Infrastructure/DependencyInjection.cs
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

### 2. POST /api/procurement/orders tesztelés

```bash
TOKEN=<test-runner-token>
curl -X POST http://127.0.0.1:5006/api/procurement/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"supplierId":"<guid>","amount":1000}'
# → 200/201 elvárt (nem 500)
```

## Javasolt fix

Ha `EnableRetryOnFailure` van: **távolítsd el** (mint KERNEL-ben).

## DoD

- [ ] `POST /api/procurement/orders` → 200/201 (nem 500)
- [ ] `dotnet test` → 53+ zöld
- [ ] Commit, INFRA deploy notify

---

*Skill: `/spaceos-terminal`*
