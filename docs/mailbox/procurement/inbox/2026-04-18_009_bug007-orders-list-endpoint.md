---
id: MSG-PROCUREMENT-009
from: root
to: procurement
type: task
priority: high
status: READ
created: 2026-04-18
---

# BUG-007 — GET /api/procurement/orders list endpoint hiányzik

## Szimptóma (böngésző console)

```
bff/procurement/orders: Failed to load resource: 405 (Method Not Allowed)
```

## Gyökérok

`ProcurementEndpoints.cs` csak ezeket regisztrálja:
```csharp
group.MapPost("/orders", CreatePurchaseOrder);         // ✅ van
group.MapGet("/orders/{id:guid}", GetOrderStatus);     // ✅ van
// GET /orders (lista) → HIÁNYZIK ❌
```

A UI `GET /bff/procurement/orders` → `GET /api/procurement/orders` hívja listázáshoz → 405.

## Feladat

### 1. GET /api/procurement/orders list endpoint hozzáadása

`ProcurementEndpoints.cs`-ben:

```csharp
group.MapGet("/orders", GetAllOrders);
```

Handler (a `GetOrderStatus`-hoz hasonlóan, de lista):

```csharp
private static async Task<IResult> GetAllOrders(
    IProcurementRepository repo,
    CancellationToken ct)
{
    var orders = await repo.GetAllOrdersAsync(ct).ConfigureAwait(false);
    return Results.Ok(orders);
}
```

`IProcurementRepository`-ba és implementációba is add hozzá a `GetAllOrdersAsync` metódust — tenant-scope alapján (RLS / TenantSessionInterceptor már él).

### 2. DTO-t visszaadni (meglévő DTO-t használd)

A UI `PurchaseOrderDto`-t vár:
```ts
interface PurchaseOrderDto {
  id: string;
  supplierName: string;
  totalAmount: number;
  expectedDelivery: string;
  status: string;
  createdAt: string;
}
```

Ha `PurchaseOrderDto` nincs `supplierName` mezővel a Procurement modulban, JOIN-old a `Supplier` táblát.

## DoD

- [ ] `GET /api/procurement/orders` → **200 + lista** (üres tömb is elfogadható ha nincs adat)
- [ ] `dotnet test` → összes zöld (legalább 51)
- [ ] INFRA deploy szükséges → jelezd outbox-ban

---

*Skill: `/spaceos-terminal`*
