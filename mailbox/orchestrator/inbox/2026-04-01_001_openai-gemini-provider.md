---
id: MSG-O001
from: root
to: orchestrator
type: task-assign
priority: P1
status: READ
created: 2026-04-01T18:30:00
---

## Tárgy

OpenAI-kompatibilis provider implementálása (Google Gemini támogatás)

## Feladat

Implementáld az `openai.provider.ts`-t, ami az OpenAI-kompatibilis API-t használja. Ez a Google Gemini OpenAI-kompatibilis endpointjával fog működni.

### Részletek

1. Hozd létre `src/llm/openai.provider.ts` — implementálja az `ILlmProvider` interfészt
2. Használj natív `fetch`-et (ne adj hozzá új package-et)
3. Env változók:
   - `OPENAI_API_KEY` — már van az env.ts-ben
   - `OPENAI_BASE_URL` — új, default: `https://generativelanguage.googleapis.com/v1beta/openai`
   - `OPENAI_MODEL` — új, default: `gemini-2.0-flash`
4. Frissítsd az `env.ts` schema-t az új env var-okkal
5. Frissítsd a `llm.provider.ts` factory switch-et
6. Frissítsd a `.env.example`-t
7. Error handling: ne dobj exception-t — return `finishReason: 'error'`

### OpenAI Chat Completions API format

```
POST {baseUrl}/chat/completions
Headers: Authorization: Bearer {apiKey}, Content-Type: application/json
Body: { model, messages, tools, max_tokens }
```

Tool schema mapping (Anthropic → OpenAI):
```
Anthropic: { name, description, input_schema }
OpenAI:    { type: "function", function: { name, description, parameters } }
```

### .env konfiguráció Gemini-hez

```
LLM_PROVIDER=openai
OPENAI_API_KEY=<google-api-key>
OPENAI_BASE_URL=https://generativelanguage.googleapis.com/v1beta/openai
OPENAI_MODEL=gemini-2.0-flash
```

## Pipeline

CODE → TEST. Outbox status-update.
