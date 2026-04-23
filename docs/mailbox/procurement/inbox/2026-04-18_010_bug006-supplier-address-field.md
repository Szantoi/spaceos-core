---
id: MSG-PROCUREMENT-010
from: root
to: procurement
type: task
priority: medium
status: READ
created: 2026-04-18
---

# BUG-006 — Szállítók: Cím mező nem mentődik

## Szimptóma

A Portal `CreateSupplierModal` küld `address` mezőt, de a backend silently eldobja:

```ts
// CreateSupplierRequest (Portal) — address MEGVAN:
{ name, email, phone, address }

// CreateSupplierCommand (Procurement backend) — address HIÁNYZIK:
new CreateSupplierCommand(tenantId, request.Name, request.Email, request.Phone)
// ^ address nem kerül bele!
```

## Javítandó fájlok

### 1. `CreateSupplierRequest` record — add Address mező

```csharp
// Jelenleg:
public sealed record CreateSupplierRequest(string Name, string? Email, string? Phone, string? Notes);

// Fix:
public sealed record CreateSupplierRequest(string Name, string? Email, string? Phone, string? Address, string? Notes);
```

### 2. `CreateSupplierCommand` — add Address

```csharp
new CreateSupplierCommand(tenantId, request.Name, request.Email ?? string.Empty, request.Phone ?? string.Empty, request.Address ?? string.Empty)
```

### 3. `Supplier` domain entity — ellenőrizd van-e `Address` property

Ha nincs → add hozzá. Ha van → csak a command-on és request-en kell átvezetni.

### 4. `CreateSupplierCommandHandler` — az Address-t persist-elje

A `Supplier` entity `Address` property-jét a command.Address értékéből állítsa be.

## DoD

- [ ] Szállító felvételekor a Cím mező megjelenik a listában
- [ ] `dotnet test` → legalább 53 zöld
- [ ] INFRA deploy szükséges → jelezd

---

*Skill: `/spaceos-terminal`*
