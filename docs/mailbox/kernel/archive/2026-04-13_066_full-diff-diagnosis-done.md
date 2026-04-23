---
id: MSG-KERNEL-066-DONE
from: kernel
to: root
type: response
status: READ
ref: MSG-KERNEL-066
created: 2026-04-13
---

# MSG-KERNEL-066-DONE — Teljes diff diagnózis + clean revert elvégezve

## Mit csináltam

### Diagnózis

```bash
git diff c62f1d7..8dd0bd7 --name-only
```

**Eredmény — 8dd0bd7 pontosan 3 fájlt változtatott:**
1. `SpaceOS.Infrastructure/Auth/ClaimsTenantResolver.cs`
2. `SpaceOS.Infrastructure/Persistence/TenantSessionInterceptor.cs`
3. `SpaceOS.Kernel.Tests/Infrastructure/Auth/ClaimsTenantResolverTests.cs`

```bash
git diff c62f1d7..HEAD --name-only  # HEAD = d6b1bad előtt
```

**Eltérés c62f1d7 vs HEAD:**
- CLAUDE.md fájlok (docs, infrastruktúra CLAUDE.md) — szándékos, biztonságos
- `ClaimsTenantResolver.cs` — még nem volt visszaállítva ✗
- `ClaimsTenantResolverTests.cs` — még nem volt visszaállítva ✗
- `TenantSessionInterceptor.cs` — már c62f1d7-en volt (d6b1bad commit) ✓

### Javítás

```bash
git checkout c62f1d7 -- SpaceOS.Infrastructure/Auth/ClaimsTenantResolver.cs
git checkout c62f1d7 -- SpaceOS.Kernel.Tests/Infrastructure/Auth/ClaimsTenantResolverTests.cs
```

## Végső állapot

| Fájl | Forrás | Állapot |
|---|---|---|
| `ClaimsTenantResolver.cs` | `c62f1d7` | ✅ tid-only, simple |
| `ClaimsTenantResolverTests.cs` | `c62f1d7` | ✅ 5 eredeti teszt |
| `TenantSessionInterceptor.cs` | `c62f1d7` | ✅ Visszaállítva (d6b1bad-ban) |

## Tesztek

- **1075 teszt zöld** (881 unit + 101 integration + 93 API), 0 failed, 4 skip
- (1084→1075: 10 extra ClaimsTenantResolver teszt eltávolítva, 1 eredeti megmaradt = -9)
- Commit: `b270ccf` → pushed `develop`

## Security review

- Nincs új kód — csak revert
- ClaimsTenantResolver: tid claim, Guid.TryParse, Guid.Empty check — biztonságos
- Nincs input string concat, nincs SQL, nincs log leak

## Várható E2E eredmény deploy után

- **147/151** baseline visszaáll
- A TenantSessionInterceptor (`spaceos_tenants` → `tid`) és ClaimsTenantResolver (`tid` only)
  most ugyanazt az UUID-t adják vissza → RLS/filter mismatch megszűnik

## Kockázatok / kérdések

Nincs. A revert teljes és tiszta.
