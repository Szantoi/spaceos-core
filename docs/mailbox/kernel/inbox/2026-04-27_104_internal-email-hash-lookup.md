---
id: MSG-KERNEL-104
from: root
to: kernel
type: task
priority: high
status: READ
ref: SpaceOS_PartnerTier_Architecture_v1.md
created: 2026-04-27
---

# KERNEL-104 — Internal email-hash lookup endpoint (PartnerTier blocker)

> **Tervdok:** `docs/architecture/SpaceOS_PartnerTier_Architecture_v1.md`
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Effort:** ~0.5 nap

## Feladat

A PartnerTier `AttributionWorker` a Kernel-ből kér email-hash lookup-ot. Új internal endpoint:

```
GET /internal/tenants/by-email-hash?hash={sha256_hex}
Header: X-SpaceOS-Internal: true

→ 200: { "tenantId": "uuid" }
→ 404: tenant not found
```

A Kernel `Tenants` táblában az email hash-ből megkeresi a tenant-et.

## Tesztek (+3)

1. Valid hash → 200 + tenantId
2. Unknown hash → 404
3. Missing X-SpaceOS-Internal → 403

## Definition of Done

- [ ] `GET /internal/tenants/by-email-hash` endpoint
- [ ] X-SpaceOS-Internal guard
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` ≥ 1161 pass
- [ ] Outbox DONE
