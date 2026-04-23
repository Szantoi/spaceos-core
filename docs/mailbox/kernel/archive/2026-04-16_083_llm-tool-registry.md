---
id: MSG-KERNEL-083
from: root
to: kernel
type: task
priority: medium
status: READ
ref: KERNEL-069
created: 2026-04-16
---

# MSG-KERNEL-083 — LLM Tool Registry endpoint (KERNEL-069)

## Kontextus

Az Orchestrator LLM Tool Calling architektúrához (5 Golden Rule #1: Data → Rules → Geometry)
szükséges egy valódi tool descriptor registry a Kernel-ben.

Az E2E-015 diagnózisa alapján a jelenlegi `/api/tools/*` endpointok adat-projekciók
(paginated queries), nem LLM tool descriptors.

## Feladat

Implementálj egy új endpointot:

```
GET /api/llm-tools
Authorization: Bearer <JWT>
→ 200 OK

[
  {
    "name": "create_facility",
    "description": "Creates a new facility for the current tenant",
    "parameters": {
      "type": "object",
      "properties": {
        "name": { "type": "string", "description": "Facility name" }
      },
      "required": ["name"]
    }
  },
  {
    "name": "get_facilities",
    "description": "Lists all facilities for the current tenant",
    "parameters": { "type": "object", "properties": {} }
  },
  {
    "name": "submit_door_order",
    "description": "Submits a door manufacturing order to Joinery",
    "parameters": {
      "type": "object",
      "properties": {
        "facilityId": { "type": "string", "format": "uuid" },
        "items": { "type": "array", "items": { "type": "object" } }
      },
      "required": ["facilityId", "items"]
    }
  }
]
```

## Implementációs irány

- Új endpoint: `GET /api/llm-tools` — `AllowAnonymous` VAGY `RequireAuthorization`
  (döntés: ha az Orchestrator service-to-service hívja, Bearer token lesz; ha publikus discovery, AllowAnonymous)
- A tool lista statikus / hard-coded a kezdeti fázisban — dinamikus registry Q4 scope
- JSON Schema `parameters` mezők a tool calling OpenAI function spec szerint
- Minimum 3-5 tool descriptor a Doorstar use case-hez

## Tesztek

- Legalább 3 teszt: 200 response, lista nem üres, tool struktúra valid
- Meglévő 1110 teszt ne törjön

## DoD

- [ ] `GET /api/llm-tools` → 200, valid tool descriptor lista
- [ ] Build: 0 error, 0 warning
- [ ] Tesztek: ≥1110 pass (+ új tesztek)
- [ ] Commit + outbox: `MSG-KERNEL-083-DONE`
