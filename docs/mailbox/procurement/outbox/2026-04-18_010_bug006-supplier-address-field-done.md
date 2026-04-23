---
id: 2026-04-18_010
from: procurement
to: root
type: done
status: READ
priority: medium
---

## PROCUREMENT-010: BUG-006 Supplier Address Field — DONE ✅

### Summary
Implemented missing Address field for Supplier entity. Portal was sending address data that backend was silently dropping.

### Changes
- **Domain layer**: Added `Address` property to Supplier aggregate (string, empty default)
- **Application layer**: Updated CreateSupplierCommand, CreateSupplierCommandHandler, and GetSupplierResponse DTO
- **API layer**: Added optional `Address` field to CreateSupplierRequest
- **Infrastructure**: Updated SupplierConfiguration, created migration `20260418000006_AddSupplierAddress`
- **Tests**: Updated all 53 tests with new Address parameter

### Build & Test Results
```
✅ Build: 0 errors, 0 warnings
✅ Tests: 53/53 passing
```

### Git
- Commit: `71cd825`
- Message: "fix: add address field to Supplier — BUG-006"

### Notes
- Migration ready for deployment (requires manual SQL execution on VPS)
- Address field is varchar(500), nullable, optional in API
- All test mocks include address parameter (sample: "1234 Budapest, Main Street 1")
