---
id: P0-1
title: JWT RS256 Authority fix — Cutting, Inventory, Procurement
status: active
priority: critical
assignee: kernel
epic: security
blocked_by: —
created: 2026-05-27
updated: 2026-05-27
docs:
  - docs/mailbox/kernel/inbox/2026-05-27_106_p0-1-jwt-rs256-fix.md
---

# P0-1 — JWT RS256 Authority fix

## Probléma

Cutting, Inventory, Procurement `Program.cs`-ben `AddJwtBearer` nincs `Authority` + `ValidateIssuer` konfigurálva.
Bármely formailag valid JWT elfogadott → `tid` claim hamisítható → cross-tenant hozzáférés lehetséges.

## Érintett service-ek

- `backend/spaceos-modules-cutting` — `src/SpaceOS.Modules.Cutting.Api/Program.cs:22`
- `backend/spaceos-modules-inventory` — `src/SpaceOS.Modules.Inventory.Api/Program.cs:10`
- `backend/spaceos-modules-procurement` — `src/SpaceOS.Modules.Procurement.Api/Program.cs:9`

## Fix

Authority-alapú JWKS validáció hozzáadása (Joinery/Kernel minta alapján).

## Unblock

Ez zárja fel az Identity modul production deploy-ját (`INFRA-IDENTITY-DEPLOY`).
