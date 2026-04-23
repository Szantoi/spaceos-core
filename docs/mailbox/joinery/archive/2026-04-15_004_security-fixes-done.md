---
id: MSG-JOINERY-005-DONE
from: joinery
to: root
type: response
priority: high
status: READ
ref: MSG-JOINERY-005
created: 2026-04-15
---

# MSG-JOINERY-005-DONE — Security fixes: M1 + M2 + M3

## Összefoglaló

Minden DoD pont teljesítve. Commit: `27b9cfb` (main branch).

⚠️ **Megjegyzés**: A task `push develop`-ot kért, de `develop` branch nem létezik — csak `main`. Push nem futott (nincs remote origin konfigurálva). Root döntést kér: push main, vagy develop branch létrehozás szükséges?

---

## Implementált változások

### M1 — pageSize clamp ✅ (előző session + commit)

```csharp
// DoorOrderEndpoints.cs
var clampedPageSize = Math.Clamp(pageSize, 1, 100);
var result = await mediator.Send(new ListDoorOrdersQuery(tenantId, page, clampedPageSize), ct)
```

### M2 — ValidateAudience = true ✅

```csharp
// Program.cs
opts.TokenValidationParameters = new TokenValidationParameters
{
    ValidateAudience = true,
    ValidAudience    = jwtAudience,  // "kernel-api"
};
```

A `JoineryWebFactory` (integration tests) `PostConfigure`-ban `ValidateAudience = false`-ra override-olja — tesztek nem törtek el.

### M3 — Startup fail-fast (Production) ✅

```csharp
// Program.cs
var jwtAuthority = builder.Configuration["Jwt:Authority"]
    ?? Environment.GetEnvironmentVariable("JWT_AUTHORITY");

if (builder.Environment.IsProduction())
    ArgumentNullException.ThrowIfNullOrEmpty(jwtAuthority,
        "Jwt:Authority / JWT_AUTHORITY must be configured");
```

Csak Production environmentben dobja — Development/Testing nem érinti (appsettings.json-ban `"Authority": ""` van).

---

## Build & Test

```
dotnet build  → 0 error, 0 warning
dotnet test   → 202/202 passed
```

Commit: `27b9cfb fix(security): JOINERY-M1/M2/M3 + Sprint 5 test coverage`

---

## Nyitott kérdés

`develop` branch nem létezik a repóban (csak `main`). Push-hoz root döntés szükséges:
1. Push `main`-re (`git push origin main`)
2. `develop` branch létrehozása + push
