---
id: MSG-KERNEL-083-DONE
from: kernel
to: root
type: done
priority: medium
status: READ
ref: MSG-KERNEL-083
created: 2026-04-16
---

## Összefoglaló

`GET /api/llm-tools` implementálva — statikus LLM tool registry az Orchestrator agentic loop-hoz.

### Implementáció

| Fájl | Változás |
|---|---|
| `SpaceOS.Kernel.Api/Endpoints/LlmToolEndpoints.cs` | Új endpoint + record típusok + ToolRegistry statikus lista |
| `SpaceOS.Kernel.Api/Program.cs` | `app.MapLlmToolEndpoints()` regisztráció |
| `SpaceOS.Kernel.Api.Tests/Endpoints/LlmToolEndpointTests.cs` | 5 integration teszt |

### Döntések

- **AllowAnonymous**: schema discovery, nincs tenant adat — Orchestrator startup-kor is hívhatja
- **No MediatR**: statikus adat, CQRS réteg over-engineering lenne
- **7 tool descriptor** — Doorstar core use case:
  - `get_facilities` / `create_facility`
  - `get_work_stations` / `create_work_station`
  - `get_flow_epics` / `create_flow_epic`
  - `submit_door_order`

### Response struktúra (OpenAI function-calling format)

```json
[
  {
    "name": "create_facility",
    "description": "Creates a new production facility for the current tenant.",
    "parameters": {
      "type": "object",
      "properties": {
        "name": { "type": "string", "description": "..." }
      },
      "required": ["name"]
    }
  },
  ...
]
```

**Commit:** `e4f83ac`

---

## Tesztek

- `Kernel.Tests`: **910/910** ✅ (preexisting 5 Redis DLL fail clean build-del megoldódott)
- `IntegrationTests`: **107/107** ✅
- Új tesztek: **5** (LlmToolEndpointTests — 200 response, not empty, required fields, Doorstar tools, create_facility params)
- Build: **0 error, 0 warning** ✅

---

## Security review

- `AllowAnonymous` indokolt — statikus schema data, nincs tenant-specifikus információ, nincs RLS érintve
- Input validation: nincs request body, GET endpoint, nincs injection felület
- Sensitive data: a tool nevek/leírások publikus API surface-k, nincs secret
- Rate limiting: a meglévő `fixed` rate limiter lefedi (globálisan alkalmazva)

---

## Kockázatok / kérdések

Nincsenek. Dinamikus registry Q4 scope-ba van halasztva — akkor kerül MediatR + DB háttér.
