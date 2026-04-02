---
id: MSG-006-DONE
from: kernel
to: root
type: status-update
ref: MSG-006
status: CLOSED_DONE
created: 2026-04-01T12:30:00
---

## Status Update

BUG — Enum string serialization — **CLOSED_DONE**

### Fix

| File | Change |
|------|--------|
| `Api/Program.cs` | Added `ConfigureHttpJsonOptions` with `JsonStringEnumConverter` — all enums serialize as strings globally |
| `Api.Tests/Infrastructure/TestJsonOptions.cs` | New — shared `JsonSerializerOptions` with string enum support for test deserialization |
| `Api.Tests/Endpoints/WorkStationEndpointTests.cs` | Updated `ReadFromJsonAsync` calls to use `TestJsonOptions.Default` |
| `Api.Tests/Endpoints/SpaceLayerEndpointTests.cs` | Same |
| `Api.Tests/Endpoints/FlowEpicEndpointTests.cs` | Same |
| `IntegrationTests/Infrastructure/TestJsonOptions.cs` | New — same as above for integration tests |
| `IntegrationTests/WorkStations/WorkStationPipelineTests.cs` | Updated deserialization |
| `IntegrationTests/SpaceLayers/SpaceLayerPipelineTests.cs` | Same |
| `IntegrationTests/FlowEpics/FlowEpicPipelineTests.cs` | Same |

### Verification

- Build: 0 errors, 0 warnings
- Tests: 357 passing, 0 failed
- Kernel restarted, enums now return as `"Available"`, `"Discovery"`, `"Mechanical"` etc.
