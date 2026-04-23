---
id: MSG-INVENTORY-058
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-ARCH-004-RESPONSE
created: 2026-04-20
---

# INVENTORY-015 — Batch offcut endpoint

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Spec:** `docs/architecture/ADR-038-offcut-creation-at-plan-freeze.md`
> **Timeline:** ~0.5 nap
> **Blokkoló:** — (párhuzamosan futhat NESTING-001-gyel)

---

## Kontextus

A Cutting modul a CuttingPlan Freeze-elésekor batch-ben regisztrálja az offcut darabokat az Inventory-ban. Az offcut loop lezárásához szükséges egy dedikált batch endpoint idempotency constraint-tel, hogy a Plan kétszeri Freeze-elése ne duplikáljon Offcut rekordokat.

---

## Feladatok

### 1. POST /api/inventory/offcuts/batch endpoint

**Fájl:** `Api/Endpoints/OffcutEndpoints.cs` (vagy ahol az offcut endpoints vannak)

```csharp
app.MapPost("/api/inventory/offcuts/batch", RegisterOffcutBatch)
    .RequireAuthorization("ManufacturerOnly");
```

**Request body:**
```csharp
public sealed record RegisterOffcutBatchRequest(
    string SourceType,       // "CuttingPlan"
    Guid SourceId,           // PlanId — idempotency key
    IReadOnlyList<OffcutItemRequest> Items
);

public sealed record OffcutItemRequest(
    string MaterialCode,
    decimal WidthMm,
    decimal HeightMm,
    decimal X,
    decimal Y
);
```

**Idempotency logika:** Upsert by `(TenantId, SourceType, SourceId)` — ha már létezik ugyanez a batch, visszaadja az eredeti ID-kat, nem hoz létre duplikátumot.

**Response:**
- `201 Created` — új batch regisztrálva
- `200 OK` — idempotens ismétlés (ugyanaz a batch már létezett)

### 2. Migration

```bash
dotnet ef migrations add AddOffcutIdempotencyConstraint \
  --project SpaceOS.Modules.Inventory.Infrastructure \
  --startup-project SpaceOS.Modules.Inventory.Api
```

Unique constraint: `(TenantId, SourceType, SourceId)` az offcut batch-eken.

Ha a meglévő `Offcut` táblastruktúra nem tartalmazza a `SourceType` / `SourceId` mezőket, add hozzá ebben a migrationban.

---

## Tesztek

Legalább 4 új teszt:
- `POST /api/inventory/offcuts/batch` → 201 Created (új batch)
- Ugyanaz a batch kétszer → 200 OK + eredeti ID-k (idempotens)
- `POST /api/inventory/offcuts/batch` auth nélkül → 401
- Üres `Items` lista → 400 Bad Request (validáció)

Meglévő tesztek mind zölden.

---

## Definition of Done

- [ ] `POST /api/inventory/offcuts/batch` endpoint → 201/200 idempotens
- [ ] Migration: `(TenantId, SourceType, SourceId)` unique constraint
- [ ] ≥4 új teszt
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld
- [ ] Outbox DONE üzenet küldve
