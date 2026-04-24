---
id: MSG-JOINERY-054
from: root
to: joinery
type: task
priority: high
status: READ
ref: SpaceOS_Joinery_Phase3_Architecture_v1.md
created: 2026-04-24
---

# JOINERY-054 — MinIO presigned URL host fix (Phase 3 előfeltétel)

> **Tervdok:** `docs/architecture/SpaceOS_Joinery_Phase3_Architecture_v1.md` — KÖTELEZŐ olvasmány!
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Effort:** ~0.25 nap
> **Használhatsz sub-agent-eket** ha szükséges

---

## Probléma

A MinIO `127.0.0.1:9000`-on hallgat. A Joinery API presigned URL-eket generál a batch PDF és anyaglista letöltéshez. Ezek `http://127.0.0.1:9000/...` formátumúak — a böngésző nem éri el.

## Fix

A MinIO client endpoint konfigurációját módosítani kell, hogy a presigned URL publikus domain-re mutasson.

1. **Ellenőrizd** hogyan van konfigurálva a MinIO client az `appsettings.json`-ban vagy DI-ban:
```bash
grep -r "MinIO\|Minio\|minio\|Endpoint\|PresignedUrl\|S3" src/ --include="*.cs" --include="*.json" | grep -v obj/ | grep -v bin/
```

2. **Konfiguráció bővítés:** A presigned URL base URL-jének konfigurálhatónak kell lennie:
```json
{
  "GyartasilapStorage": {
    "Endpoint": "127.0.0.1:9000",
    "PublicEndpoint": "https://joinerytech.hu/minio"
  }
}
```

Vagy environment variable: `GyartasilapStorage__PublicEndpoint=https://joinerytech.hu/minio`

3. **A presigned URL generálásnál** a belső endpoint helyett a PublicEndpoint-ot használd:
```csharp
// A presigned URL host-ját cseréld ki:
// http://127.0.0.1:9000/bucket/key?signature=...
// → https://joinerytech.hu/minio/bucket/key?signature=...
```

**Megjegyzés:** Az nginx MinIO proxy route (`/minio/`) az INFRA feladatban kerül beállításra — az a te feladatodtól független. Te csak a Joinery kódot módosítsd hogy a publikus URL-t generálja.

## Tesztek (+2)

1. Presigned URL tartalmazza a PublicEndpoint-ot (nem `127.0.0.1:9000`)
2. Ha PublicEndpoint nincs konfigurálva → fallback a belső Endpoint-ra (backward compatible)

## Definition of Done

- [ ] Presigned URL konfigurálható PublicEndpoint-tal
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 389 pass
- [ ] Outbox DONE
