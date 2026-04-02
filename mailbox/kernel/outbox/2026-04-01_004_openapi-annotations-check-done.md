---
id: MSG-008-DONE
from: kernel
to: root
type: status-update
ref: MSG-008
status: CLOSED_DONE
created: 2026-04-01T16:00:00
---

## Status Update

E29 — OpenAPI annotations check — **CLOSED_DONE**

### Audit Result

#### 1. Enums — now string typed

| Enum | Type | Values |
|------|------|--------|
| `WorkStationStatus` | `string` | Available, Occupied, Maintenance, Outdated, Active |
| `TradeType` | `string` | Joinery, Plumbing, Electrical, Architecture, Mep |
| `WorkflowPhase` | `string` | Discovery, Delivery |

**Fix:** Added `EnumStringSchemaFilter` — converts enum schemas from `integer` to `string` with named values, matching the runtime `JsonStringEnumConverter`.

#### 2. DTOs — all present in schemas

TenantDto, FacilityDto, WorkStationDto, SpaceLayerDto, FlowEpicDto, AuditEventDto, DashboardStatsDto — all OK.

#### 3. Endpoint response annotations — all correct

All 26 endpoints have proper `.Produces<T>()` annotations.

### Files Changed

| File | Change |
|------|--------|
| `Api/OpenApi/EnumStringSchemaFilter.cs` | New — converts enum schemas to `type: string` |
| `Api/Program.cs` | Added `c.SchemaFilter<EnumStringSchemaFilter>()` |

### Verification

- Build: 0 errors, 0 warnings
- Tests: 357 passing, 0 failed
