---
id: MSG-KERNEL-088
from: root
to: kernel
type: task
priority: critical
status: READ
ref: SPRINT6
created: 2026-04-17
---

# KERNEL-088 — ESCROW-GA: Audit event WORM streaming (MinIO)

## Kontextus

INFRA-152 DONE: MinIO live, `spaceos-audit-worm` bucket Object Lock COMPLIANCE 365 nap.

```
Endpoint:   http://127.0.0.1:9000
Access Key: spaceos-minio
Secret Key: /etc/spaceos/minio.env-ből (MINIO_ROOT_PASSWORD)
Bucket:     spaceos-audit-worm
```

Az audit chain minden eventjét a MinIO bucket-be kell írni — megvalósítva a WORM
(Write Once Read Many) escrow követelményt. Ha a PostgreSQL audit tábla sérül,
a MinIO tartalom az egyetlen hiteles forrás marad (365 napig törölhetetlen).

## Tudásbázis referencia

- `docs/knowledge/context/KERNEL_CONTEXT.md` — terminál kontextus
- `docs/knowledge/security/SECURITY_PATTERNS.md` — audit chain hash szerkezet
- `docs/knowledge/patterns/DATABASE_PATTERNS.md` — AuditDbContext minta

## Feladat

### 1. MinIO NuGet package

```xml
<PackageReference Include="AWSSDK.S3" Version="3.*" />
```

A MinIO S3-kompatibilis API-t használ — az AWS S3 SDK működik ellene.
Vagy: `Minio` official .NET SDK (MIT). Válassz és indokold az outboxban.

### 2. IAuditEscrowWriter interfész (Domain/Infrastructure boundary)

```csharp
// SpaceOS.Infrastructure/AuditLog/IAuditEscrowWriter.cs
public interface IAuditEscrowWriter
{
    Task WriteAsync(AuditEvent auditEvent, CancellationToken ct = default);
}
```

### 3. MinioAuditEscrowWriter implementáció

Object key séma:
```
{tenantId}/{year}/{month}/{eventId}.json
pl: 79d71e39-5571-43d8-9f07-5f7631afa5e7/2026/04/evt-abc123.json
```

Object content: `AuditEvent` JSON serializálva.

Object metadata:
```
x-amz-meta-audit-chain-hash: {hash}
x-amz-meta-created-at:       {timestamp ISO8601}
x-amz-meta-tenant-id:        {tenantId}
```

Idempotencia: `PUT if-none-match: *` — ha az object már létezik, ne írjon felül.

Hibakezelés: ha a MinIO write sikertelen, **NE blokkolja** az audit write DB-be —
fire-and-forget pattern, de logoljon hibát.

### 4. Bekötés az audit write pipeline-ba

Az `AuditEventRepository.AddAsync` (vagy az advisory lock write path) után hívja
a `IAuditEscrowWriter.WriteAsync`-t. A DB write primér, a MinIO write szekunder.

### 5. Konfiguráció

`appsettings.json` / env:
```json
"MinioEscrow": {
  "Endpoint": "http://127.0.0.1:9000",
  "AccessKey": "spaceos-minio",
  "SecretKey": "",        // env-ből: MinioEscrow__SecretKey
  "BucketName": "spaceos-audit-worm",
  "Enabled": true
}
```

`/etc/spaceos/kernel.env`-be: `MinioEscrow__SecretKey=<MINIO_ROOT_PASSWORD>`

### 6. Tesztek

- Unit: `MinioAuditEscrowWriter` mock S3 client-tel (object key helyes, metadata kész)
- Unit: ha MinIO dob, az audit DB write nem sérül (fire-and-forget igazolás)
- Integration: opcionális (Testcontainers MinIO image), ha az idő engedi
- Tesztszám: ≥ 1135 (jelenlegi 1122 + min. +13)

## Build gate

```bash
dotnet test --no-build --verbosity minimal
# 0 fail, min 1135 pass
```

## DONE feltételek

- [ ] `IAuditEscrowWriter` + `MinioAuditEscrowWriter` implementálva
- [ ] Audit write pipeline-ba bekötve (fire-and-forget, DB write nem blokkolódik)
- [ ] Object key: `{tenantId}/{year}/{month}/{eventId}.json`
- [ ] Idempotens: meglévő object nem kerül felülírásra
- [ ] `MinioEscrow__SecretKey` env változóból olvas
- [ ] Tesztszám ≥ 1135
- [ ] Commit hash
- [ ] OUTBOX DONE: SDK választás indoklás + tesztszám + manuális verify (mc ls)

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent **engedélyezett**.
