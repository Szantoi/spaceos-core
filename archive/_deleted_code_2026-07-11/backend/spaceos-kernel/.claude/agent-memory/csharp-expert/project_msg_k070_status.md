---
name: MSG-KERNEL-070 Ecosystem Actor Architecture v4 status
description: MSG-KERNEL-070 TenantType + ModuleRegistry + Migration 0029 CLOSED_DONE — 1104 tests passing (2026-04-15)
type: project
---

MSG-KERNEL-070 — Ecosystem Actor Architecture v4 CLOSED_DONE.

**Why:** Tenant ecosystem actor classification (Manufacturer, PanelCutter, Trader, Logistics, Installer, EndCustomer) needed for B2B handshake graph and module permission enforcement.

**How to apply:** TenantType is immutable after creation (DB trigger SEC-01 + Application layer). Module validation happens at both Application (IModuleRegistryService) and DB (trigger SEC-02) levels. Use `Tenant.Register()` for new code, `Tenant.Create()` kept for backwards compat.

Key decisions:
- `ModuleValidationResult` value type in Domain (not Ardalis.Result — Domain has no Ardalis.Result dep)
- `ModuleRegistryService` lives in Domain (stateless, no infra deps)
- `IModuleRegistryService` registered as Singleton in Infrastructure DI
- TenantDto now includes `TenantType` (string) + `EnabledModules`
- Test count: 1077 → 1104 passing (27 new tests)
