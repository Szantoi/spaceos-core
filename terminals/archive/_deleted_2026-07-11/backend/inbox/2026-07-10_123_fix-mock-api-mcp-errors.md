---
id: MSG-BACKEND-123
from: root
to: backend
type: task
priority: high
status: READ
model: sonnet
created: 2026-07-10
---

# Fix JoineryTech Mock API + MCP Tool Errors

## Kontextus

A JoineryTech UI modulok "Loading..." state-ben ragadnak, mert a mock API nincs integrálva. Emellett több MCP tool is hibás.

## 1. Mock API Integráció

### Probléma
- `datahaven-web/client/src/hooks/useCRM.ts` direkten `/api/crm`-re fetch-el
- `datahaven-web/client/src/services/mockCrmApi.ts` létezik, de nincs használva
- `VITE_USE_MOCK_API=true` be van állítva, de a hooks nem ellenőrzik

### Megoldás
Módosítsd a `useCRM.ts`-t (és hasonló hook-okat), hogy mock módban a mockCrmApi-t használják:

```typescript
import { USE_MOCK_API, mockCrmApi } from '@/services/mockCrmApi';

const fetchLeads = async (filters?: LeadFilters): Promise<LeadSummaryDto[]> => {
  if (USE_MOCK_API) {
    const leads = mockCrmApi.getLeads();
    // Transform to DTO format if needed
    return leads.map(l => ({ ...l }));
  }

  // Original fetch code...
  const response = await fetch(`${API_BASE}/leads?${params}`);
  // ...
};
```

### Érintett fájlok
- `datahaven-web/client/src/hooks/useCRM.ts`
- Esetleg: useKontrolling, useHR, useMaintenance, useQA, useDMS (ha léteznek)

## 2. SSE Kapcsolat Hiba

### Probléma
`datahaven-web/client/src/services/sseClient.ts` folyamatosan reconnect-el → "Kapcsolat megszakadt"

### Megoldás
```typescript
// sseClient.ts
import { USE_MOCK_API } from '@/services/mockCrmApi';

export class SSEClient {
  connect() {
    if (USE_MOCK_API) {
      console.log('[SSE] Mock mode - skipping real connection');
      this.simulateMockEvents();
      return;
    }
    // Original connect code...
  }

  private simulateMockEvents() {
    // Optional: emit mock events periodically
  }
}
```

## 3. MCP Tool Hibák (Nexus - külön task ha kell)

### Hibás tool-ok
- `create_goal` → Internal error -32603
- `subscribe_to_terminal` → Internal error -32603

### Valószínű ok
A root terminál nincs a `.mcp-tokens` fájlban, ezért a caller terminal ellenőrzés sikertelen.

### Fájl
`spaceos-nexus/knowledge-service/.mcp-tokens` - root token hozzáadása

## Acceptance Criteria

- [ ] `useCRM.ts` használja a mockCrmApi-t ha `USE_MOCK_API=true`
- [ ] CRM Leads oldal mock adatokat mutat (25 lead a mockCrmApi-ból)
- [ ] SSE client nem dob hibát mock módban
- [ ] "Kapcsolat megszakadt" üzenet eltűnik vagy "Mock mode" lesz

## Screenshots referencia

Lásd: `/tmp/jt-*.png` fájlok (Playwright screenshots)

| Modul | Jelenlegi státusz |
|-------|-------------------|
| CRM | "No leads found" |
| Kontrolling | "Loading..." |
| HR | "Loading employees..." |
| EHS | ✅ Működik (KPI cards) |
| Maintenance | "Loading assets..." |
| QA | "Loading inspections..." |
| DMS | "Loading documents..." |
