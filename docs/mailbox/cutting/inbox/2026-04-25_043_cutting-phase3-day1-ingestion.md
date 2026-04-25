---
id: MSG-CUTTING-043
from: root
to: cutting
type: task
priority: high
status: READ
ref: SpaceOS_Cutting_Phase3_Implementation_v1.md
created: 2026-04-25
---

# CUTTING-043 — Phase 3 Day 1-2: Order Ingestion endpoint + CuttingJob enrichment

> **Tervdok:** `docs/architecture/SpaceOS_Cutting_Phase3_Implementation_v1.md` — KÖTELEZŐ olvasmány!
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** Cutting Phase 1+2 DEPLOYED (284 teszt)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Scope (tervdok Nap 1-2)

### 1. Order Ingestion endpoint

```csharp
// POST /internal/ingest-order
// Header: X-SpaceOS-Internal: true
// Body: IngestOrderDto { orderId, tenantId, items: [{ name, widthMm, heightMm, material, grainDirection, quantity }] }
```

**Handler:** `IngestOrderCommandHandler`
- OrderId-hez CuttingJob-ok létrehozása (1 item → 1 CuttingJob)
- CuttingJob valós dimenziókkal (widthMm, heightMm — korábban stub 0 volt)
- Material + grainDirection mentése
- DaySlot hozzárendelés (legkorábbi üres slot, tervdok szerint)

### 2. CuttingJob bővítés

Ha a `CuttingJob` entity-ben hiányzik:
- `Material` (string)
- `GrainDirection` (enum: None, Vertical, Horizontal)
- `WidthMm`, `HeightMm` (int — ha eddig 0 volt)

EF migration szükséges (`dotnet ef migrations add`).

### 3. Tesztek (+8)

1. Ingest happy path: 3 item → 3 CuttingJob created
2. CuttingJob dimenziók: widthMm/heightMm > 0
3. Material + grainDirection megmarad
4. DaySlot assignment: job a legkorábbi szabad slotba kerül
5. Duplicate orderId → idempotens (nem duplikál)
6. Üres items → 400
7. Internal header hiányzik → 403
8. Tenant isolation: A tenant job-jai nem láthatók B tenant-nek

## Definition of Done

- [ ] `POST /internal/ingest-order` endpoint
- [ ] CuttingJob valós dimenziókkal
- [ ] EF migration (`dotnet ef migrations add` — NEM manuális!)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 292 pass (284 + min 8 új)
- [ ] Outbox DONE
