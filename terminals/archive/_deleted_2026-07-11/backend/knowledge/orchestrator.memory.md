# Orchestrator Domain Memory

> Automatikusan betöltődik ha a feladat Orchestrator/BFF modulhoz kapcsolódik.

## Domain Scope

- **Modul:** `spaceos-orchestrator`
- **Felelősség:** API Gateway, LLM Tool Calling, Request routing, Caching
- **Tech stack:** Node.js 22, Express, Redis, OpenAI/Anthropic SDK

## Aktív Patterns

### 1. Tool Calling Pattern
```typescript
interface ToolDefinition {
  name: string;
  description: string;
  parameters: JSONSchema;
  handler: (params: unknown) => Promise<ToolResult>;
}

const tools: ToolDefinition[] = [
  {
    name: 'calculate_door_price',
    description: 'Calculate price for door configuration',
    parameters: {
      type: 'object',
      properties: {
        width: { type: 'number' },
        height: { type: 'number' },
        material: { type: 'string' },
        finish: { type: 'string' }
      }
    },
    handler: async (params) => {
      const response = await fetch('http://joinery-api/api/joinery/price', {
        method: 'POST',
        body: JSON.stringify(params)
      });
      return response.json();
    }
  }
];
```

### 2. Request Aggregation
```typescript
// Több backend service összefogása egy response-ba
app.get('/api/bff/order/:id', async (req, res) => {
  const [order, customer, payments] = await Promise.all([
    kernelService.getOrder(req.params.id),
    kernelService.getCustomer(order.customerId),
    kernelService.getPayments(req.params.id)
  ]);

  res.json({ order, customer, payments });
});
```

### 3. Response Caching
```typescript
const cache = new Redis(process.env.REDIS_URL);

async function cachedFetch<T>(key: string, ttl: number, fetcher: () => Promise<T>): Promise<T> {
  const cached = await cache.get(key);
  if (cached) return JSON.parse(cached);

  const data = await fetcher();
  await cache.setex(key, ttl, JSON.stringify(data));
  return data;
}
```

## API Routes

| Route | Backend Service | Cache TTL |
|-------|-----------------|-----------|
| `/api/bff/products` | Joinery | 5 min |
| `/api/bff/materials` | Cutting | 10 min |
| `/api/bff/order/:id` | Kernel + Joinery | No cache |
| `/api/bff/chat` | LLM | No cache |

## LLM Integration

```typescript
// Anthropic Claude használata
const anthropic = new Anthropic({ apiKey: process.env.ANTHROPIC_API_KEY });

async function chat(messages: Message[], tools: Tool[]): Promise<string> {
  const response = await anthropic.messages.create({
    model: 'claude-sonnet-4-20250514',
    max_tokens: 4096,
    messages,
    tools
  });

  // Tool use handling
  if (response.stop_reason === 'tool_use') {
    const toolUse = response.content.find(c => c.type === 'tool_use');
    const result = await executeToolCall(toolUse);
    return chat([...messages, { role: 'assistant', content: response.content },
                              { role: 'user', content: [{ type: 'tool_result', ...result }] }], tools);
  }

  return response.content[0].text;
}
```

## Legutóbbi Tanulságok

- **Circuit breaker** minden backend service hívásra
- **Rate limiting** LLM API-ra (cost control)
- **Retry logic** transient errors-re
- **Health check** minden downstream service-re

## Kapcsolódó Fájlok

- `spaceos-orchestrator/src/` - Node.js app
- `spaceos-orchestrator/src/tools/` - LLM tool definitions
- `spaceos-orchestrator/src/routes/` - BFF routes
- `spaceos-orchestrator/src/services/` - Backend service clients
