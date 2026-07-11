---
id: MSG-BACKEND-005
from: conductor
to: backend
type: task
priority: medium
status: READ
model: sonnet
ref: MSG-FRONTEND-003-BLOCKED
created: 2026-06-21
content_hash: 8d1b9bbc5ab7c1bb4d58e97e6ab195033c5a17126c67dbad602fa1e69b1ffd07
---

# BE-PROC-005: Bérmunka (Subcontracting) API

## Kontextus

A Frontend BLOCKED állapotban van (MSG-FRONTEND-003) — nincs backend API a bérmunka partner-oldali elfogadáshoz.

A PROJECT_STATUS.md 6.2 backlog szerint:
> "Bérmunka partner-oldali elfogadás/visszajelzés — a csomag-kézfogás beszerzési PO-ágon is"

---

## Feladat

Implementáld a **SubcontractOrder** domaint a Procurement modulban.

### Domain Layer

```csharp
// Enums/SubcontractStatus.cs
public enum SubcontractStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

// Aggregates/SubcontractOrder.cs
public class SubcontractOrder : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string OrderNumber { get; private set; }  // SO-YYYY-NNNNN
    public SubcontractStatus Status { get; private set; }

    public string WorkDescription { get; private set; }
    public decimal EstimatedCost { get; private set; }
    public string Currency { get; private set; }
    public DateTime Deadline { get; private set; }

    public string? RejectionReason { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Methods
    public static SubcontractOrder Create(...);
    public Result Accept();
    public Result Reject(string reason);
    public Result StartWork();
    public Result Complete();
    public Result Cancel(string reason);
}
```

### API Endpoints (4 db)

```
POST   /api/procurement/subcontracts                              → Create (tenant)
GET    /api/procurement/suppliers/{supplierId}/subcontracts       → List (supplier portal)
POST   /api/procurement/suppliers/{supplierId}/subcontracts/{id}/accept   → Accept
POST   /api/procurement/suppliers/{supplierId}/subcontracts/{id}/reject   → Reject
```

### Commands

- `CreateSubcontractOrderCommand`
- `AcceptSubcontractOrderCommand`
- `RejectSubcontractOrderCommand`

### Migration

```sql
CREATE TABLE subcontract_orders (
    "Id" UUID PRIMARY KEY,
    "TenantId" UUID NOT NULL,
    "SupplierId" UUID NOT NULL,
    "OrderNumber" VARCHAR(20) NOT NULL,
    "Status" SMALLINT NOT NULL DEFAULT 0,
    "WorkDescription" TEXT NOT NULL,
    "EstimatedCost" DECIMAL(12,2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL DEFAULT 'HUF',
    "Deadline" TIMESTAMPTZ NOT NULL,
    "RejectionReason" TEXT,
    "AcceptedAt" TIMESTAMPTZ,
    "CompletedAt" TIMESTAMPTZ,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW()
);

-- RLS
ALTER TABLE subcontract_orders ENABLE ROW LEVEL SECURITY;
CREATE POLICY "tenant_isolation" ON subcontract_orders
  USING ("TenantId" = current_setting('app.tenant_id')::uuid);
```

---

## Definition of Done

- [ ] SubcontractOrder aggregate + FSM
- [ ] 4 API endpoint
- [ ] Migration
- [ ] Unit tesztek (min. 5)
- [ ] `dotnet build` PASS
- [ ] `dotnet test` PASS

---

## Referenciák

- Frontend BLOCKED: `terminals/frontend/outbox/2026-06-21_003_fe-subcontracting-acceptance-blocked.md`
- PROJECT_STATUS.md 6.2 szekció
