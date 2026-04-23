---
id: MSG-K089-DONE
from: kernel
to: root
type: done
status: READ
priority: critical
ref: 2026-04-18_089_tenant-create-500-regression
---

## Összefoglaló

**KERNEL-089** — `POST /api/tenants → 500` regresszió javítva.

### Root cause

A KERNEL-088 MinIO DI regisztráció feltétel nélkül regisztrálta az `IMinioClient`
singleton factory-t produkción. A factory futáskor meghívja
`MinioClient().WithCredentials(accessKey, secretKey).Build()` — és a Minio .NET SDK 5.0.1
szinkron `MinioException`-t dob, ha az `AccessKey` vagy `SecretKey` üres string.

Produkción az `appsettings.Production.json` nem tartalmaz `MinioEscrow` szekciót,
így az értékek az `MinioEscrowOptions` defaultjaiból jönnek (üres stringek).
Az exception a DI container által feloldás közben dobódott, ezért **minden**
`AuditEventDispatcher`-t igénylő kérés 500-at adott vissza.

### Fix

3 fájl módosítva (`7f8fd4c`):

| Fájl | Változás |
|---|---|
| `SpaceOS.Infrastructure/AuditLog/MinioEscrowOptions.cs` | `Enabled` default: `true` → `false` |
| `SpaceOS.Infrastructure/DependencyInjection.cs` | MinIO DI regisztráció feltételhez kötve: csak ha `Enabled=true` ÉS credentials nem üres |
| `SpaceOS.Kernel.Api/appsettings.json` | `MinioEscrow.Enabled: false` (explicit) |

### Logika

```
if (escrowCfg.Enabled && !string.IsNullOrWhiteSpace(escrowCfg.AccessKey) && !string.IsNullOrWhiteSpace(escrowCfg.SecretKey))
  → IMinioClient + MinioStorageAdapter + MinioAuditEscrowWriter regisztrálva
else
  → NullAuditEscrowWriter regisztrálva (no-op, sosem dob)
```

Ez biztosítja, hogy az `appsettings.Production.json`-ban nem konfigurált MinIO
esetén is a `NullAuditEscrowWriter` fut, és az `AuditEventDispatcher` sikeresen
feloldható marad.

## Tesztek

**1138 / 1138 pass** (926 unit + 108 integration + 104 API, 4 skipped)

Nincs regresszió. A `MinioAuditEscrowWriterTests` (17 teszt) explicit
`Enabled=true, AccessKey="test", SecretKey="test1234"` opciókkal fut — az
`Enabled` default változás nem érinti ezeket.

## Security review

- Input validation: MinIO credentials config-ból jön, nem userspace inputból ✅
- DI guard: üres credentials esetén NullAuditEscrowWriter fut (fail-safe) ✅
- Nincs sensitive adat logban ✅
- Az escrow fire-and-forget — MinIO outage nem blokkolja az elsődleges írási utat ✅

## INFRA feladat szükséges

A fix `develop` branchen van (`7f8fd4c`). **VPS redeploy szükséges** a regresszió
éles javításához.

**DoD: `POST /api/tenants → 201` VPS-en redeploy után.**

→ INFRA terminálnak inbox üzenet szükséges a deploy elvégzéséhez.

## Kockázatok / kérdések

Ha produkción MinIO valóban konfigurálva lesz (Enabled=true + valós credentials),
a conditional registration automatikusan bekapcsolja a live escrow writert.
Migration nem szükséges — schema változás nincs.
