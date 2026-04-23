---
id: MSG-KERNEL-065-DONE
from: kernel
to: root
type: response
status: READ
ref: MSG-KERNEL-065
created: 2026-04-13
---

# MSG-KERNEL-065-DONE — TenantSessionInterceptor visszaállítva c62f1d7-re

## Mit csináltam

```bash
git checkout c62f1d7 -- SpaceOS.Infrastructure/Persistence/TenantSessionInterceptor.cs
```

A fájl pontosan a c62f1d7 állapotára állt vissza — az eredeti `string.Equals OrdinalIgnoreCase`
összehasonlítás a `X-SpaceOS-Active-Tenant` header kezelésénél. A GUID normalizálás
(`Guid.TryParse` + `Guid == Guid` összehasonlítás) eltávolítva.

## Végső állapot az ágon

| Fájl | Forrás | Állapot |
|---|---|---|
| `TenantSessionInterceptor.cs` | `c62f1d7` | ✅ Visszaállítva |
| `ClaimsTenantResolver.cs` | `3645480` | ✅ tid-first + graceful fallback (megtartva) |
| `ClaimsTenantResolverTests.cs` | `3645480` | ✅ 15 teszt (megtartva) |

## Tesztek

- **1084 teszt zöld** (890 unit + 101 integration + 93 API), 0 failed, 4 skip
- Commit: `d6b1bad` → pushed `develop`

## Várható E2E eredmény deploy után

- **147/151** baseline visszaáll (INFRA-069 + E2E-011 verifikálja)
- A maradék 4 failure (FlowEpic close, node register, summary counts) külön feladatban
