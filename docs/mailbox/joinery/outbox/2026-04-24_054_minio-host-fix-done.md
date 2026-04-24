---
id: MSG-JOINERY-054-DONE
from: joinery
to: root
type: done
priority: high
status: READ
ref: MSG-JOINERY-054
created: 2026-04-24
---

## Összefoglaló

JOINERY-054 — MinIO presigned URL PublicEndpoint fix teljes.

**Commit:** `4a2c33f`

**Módosított fájlok:**

- `Infrastructure/Storage/GyartasilapStorageOptions.cs`
  - `PublicEndpoint` property hozzáadva (nullable, backward compatible)

- `Infrastructure/Storage/GyartasilapMinioStorage.cs`
  - `ReplaceHost(presignedUrl, publicEndpoint)` internal static helper
  - `GetPresignedUrlAsync` a belső URL host-ját a `PublicEndpoint`-ra cseréli

- `Infrastructure/SpaceOS.Modules.Joinery.Infrastructure.csproj`
  - `InternalsVisibleTo` a Tests projekthez

- `Tests/Storage/GyartasilapMinioStorageTests.cs`
  - +2 teszt: host csere + fallback (null/empty/whitespace)

**Konfiguráció:** VPS-en beállítandó:
```
GyartasilapStorage__PublicEndpoint=https://joinerytech.hu/minio
```

## Tesztek

**389/389 pass** (387 → +2 új)

Utolsó sor: `Passed! - Failed: 0, Passed: 389, Skipped: 0, Total: 389, Duration: 14 s`

## Security review

- ✅ Nem érint auth/RLS logikát
- ✅ Query string (signature) megmarad a host cserékor
- ✅ Nincs TODO/FIXME

## Kockázatok / kérdések

Az nginx MinIO proxy route (`/minio/`) az INFRA feladatban kerül beállításra — az ettől a fix-től független. A Joinery kód kész a publikus endpoint használatára.

Nincsenek blokkoló kockázatok.
