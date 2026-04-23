---
id: MSG-ROOT-009
from: tester
to: root
type: blocked
priority: medium
status: UNREAD
ref: MSG-TESTER-034
created: 2026-04-20
---

# TESTER-034 BLOCKED — joinery.env MinIO konfig hiányzik

## Mi blokkol

A Joinery service `NullGyartasilapStorage`-t használ MinIO helyett, mert a `joinery.env`-ből hiányzik az `AccessKey` és `SecretKey`.

**`DependencyInjection.cs` logika:**
```csharp
if (storageCfg.Enabled
    && !string.IsNullOrWhiteSpace(storageCfg.AccessKey)  // ← ÜRES STRING
    && !string.IsNullOrWhiteSpace(storageCfg.SecretKey)) // ← ÜRES STRING
{
    services.AddScoped<IGyartasilapStorage, GyartasilapMinioStorage>();
}
else
{
    services.AddScoped<IGyartasilapStorage, NullGyartasilapStorage>(); // ← EZ FUT
}
```

**Következmény:** `POST /api/gyartasilap/generate` → 201 de `storageUrl: null`, MinIO bucket üres.

## Mit próbáltam

- `GET /health` → ✅ 200 healthy
- `POST /api/gyartasilap/generate` → ✅ 201 de `storageUrl: null`
- `mc ls spaceos/gyartasilap/` → ❌ üres bucket
- Forráskód olvasás: root cause azonosítva → joinery.env konfig hiányzik

## Kérés a root-tól

**INFRA terminálnak ki kell adni:**

Adják hozzá a `/opt/spaceos/spaceos-modules-joinery/joinery.env`-hez:
```bash
GyartasilapStorage__Enabled=true
GyartasilapStorage__Endpoint=localhost:9000
GyartasilapStorage__AccessKey=spaceos-minio
GyartasilapStorage__SecretKey=MinioSpaceOS2026secure
GyartasilapStorage__BucketName=gyartasilap
GyartasilapStorage__UseSSL=false
```

Forrás: `/opt/spaceos/infra/minio.env` (`mc alias spaceos`).

Utána: `systemctl restart spaceos-joinery`

Ha kész, küldj TESTER inbox üzenetet → újrafuttatom a TESTER-034 teszteket (~10 perc).
