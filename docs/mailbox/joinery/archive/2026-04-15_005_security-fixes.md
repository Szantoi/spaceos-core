---
id: MSG-JOINERY-005
from: root
to: joinery
type: task
priority: high
status: READ
ref: MSG-JOINERY-004-DONE
created: 2026-04-15
---

# MSG-JOINERY-005 — Security fixes: M1 commit + M2 + M3

## Feladat

A security review 3 közepes találatot azonosított. Az M1 már kész kódban — commitold.
M2 és M3 implementálandó.

---

### M1 — pageSize clamp (már implementálva — csak commit + push)

```bash
git add SpaceOS.Modules.Joinery.Api/Endpoints/DoorOrderEndpoints.cs
git commit -m "fix(joinery): pageSize clamp 1-100 (JOINERY-M1 security)"
git push origin develop
```

---

### M2 — `ValidateAudience = true`

```csharp
// Program.cs — JWT konfig
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateAudience = true,
    ValidAudience = builder.Configuration["Jwt:Audience"]
        ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
        ?? "kernel-api",
    // ... többi meglévő param
};
```

`appsettings.json`-ba (ha még nincs): `"Jwt": { "Audience": "kernel-api" }`

---

### M3 — JWT Authority startup fail-fast

```csharp
// Program.cs — startup előtt
var authority = builder.Configuration["Jwt:Authority"]
    ?? Environment.GetEnvironmentVariable("JWT_AUTHORITY");
ArgumentNullException.ThrowIfNullOrEmpty(authority,
    "Jwt:Authority / JWT_AUTHORITY must be configured");
```

---

## DoD

- [ ] M1: commit + push develop
- [ ] M2: `ValidateAudience = true` — csak valid `kernel-api` audience elfogadott
- [ ] M3: startup fail-fast ha authority hiányzik
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → 202+ teszt zöld
- [ ] Commit + push develop
