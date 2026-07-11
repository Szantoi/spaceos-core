---
id: MSG-FRONTEND-059
from: root
to: frontend
type: info
priority: medium
status: READ
model: haiku
created: 2026-06-30
read: 2026-06-30
content_hash: f16415fcec6dc198d71273d4ca1233cd281121e6ce2f37ac4b2d8e1907639acd
---

# Új Toolok Elérhetők — Frontend Terminál

2 új tool implementálva a mai napon. Használd ezeket a fejlesztéshez.

## 1. React Hook Generator

**Skill:** `/react-hook-generator`

**MCP Tool:** `generate_hook`

### Query Hook (Data Fetching)
```bash
spaceos generate hook Quotes --type query --with-cache --endpoint /api/quotes
```

Vagy MCP-n:
```json
{
  "name": "generate_hook",
  "arguments": {
    "name": "Quotes",
    "type": "query",
    "withCache": true,
    "endpoint": "/api/quotes"
  }
}
```

**Generált fájl:** `client/src/hooks/useQuotes.ts` (TanStack Query)

### Mutation Hook
```bash
spaceos generate hook SubmitQuote --type mutation --with-cache --endpoint /api/quotes/submit
```

**Generált fájl:** `client/src/hooks/useSubmitQuote.ts` (useMutation + invalidation)

### State Hook
```bash
spaceos generate hook WizardState --type state
```

**Generált fájl:** `client/src/hooks/useWizardState.ts` (useState + callbacks)

### Effect Hook
```bash
spaceos generate hook WindowResize --type effect --with-test
```

**Generált fájlok:** `useWindowResize.ts` + `useWindowResize.test.ts`

**ROI:** 80% időmegtakarítás boilerplate kódon

## 2. FSM Subscription MCP Tools

**Skill:** `/fsm-subscription`

Ha várnod kell egy másik terminál DONE-jára:

```typescript
// Feliratkozás task-ra
await mcp.subscribe_to_task({
  terminal: "frontend",
  task_id: "MSG-BACKEND-097",
  events: ["done"],
  delivery_method: "sse"
});

// SSE listener
const es = new EventSource("/api/subscriptions/events?terminal=frontend");
es.onmessage = (e) => {
  const data = JSON.parse(e.data);
  if (data.event?.name === "outbox:done") {
    console.log("Backend API ready!");
  }
};
```

## Hook Típusok

| Típus | Mikor használd | TanStack |
|-------|----------------|----------|
| `query` | Data fetching | useQuery |
| `mutation` | Data modification | useMutation |
| `state` | Local state | useState |
| `effect` | Side effects | useEffect |

## Skill Fájlok

```
.claude/skills/
├── react-hook-generator/SKILL.md
└── fsm-subscription/SKILL.md
```

Részletek: `/opt/spaceos/docs/knowledge/patterns/CODEGEN_TOOLCHAIN_PATTERN.md`
