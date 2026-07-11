# React Hook Generator Skill

> React hook-ok automatikus generálása (TanStack Query, useState, useEffect patterns).

## Mikor használd

- Új data fetching hook kell (useQuery)
- Új mutation hook kell (useMutation)
- Lokális state management hook kell
- Side effect hook kell cleanup-pal

## Használat

### CLI

```bash
spaceos generate hook <name> --type <type> [options]
```

### MCP Tool

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

## Hook Típusok

### 1. Query Hook (Data Fetching)

```bash
spaceos generate hook Quotes --type query --with-cache --endpoint /api/quotes
```

**Generált kód:**
```typescript
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api';

export function useQuotes() {
  return useQuery({
    queryKey: ['quotes'],
    queryFn: async () => {
      const response = await apiClient.get('/api/quotes');
      return response.data;
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}
```

### 2. Mutation Hook (Data Modification)

```bash
spaceos generate hook SubmitQuote --type mutation --with-cache --endpoint /api/quotes/submit
```

**Generált kód:**
```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api';

export function useSubmitQuote() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: SubmitQuoteInput) => {
      const response = await apiClient.post('/api/quotes/submit', data);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['quotes'] });
    },
  });
}
```

### 3. State Hook (Local State)

```bash
spaceos generate hook QuoteWizardState --type state
```

**Generált kód:**
```typescript
import { useState, useCallback } from 'react';

interface QuoteWizardState {
  step: number;
  data: Record<string, unknown>;
}

export function useQuoteWizardState(initialState?: Partial<QuoteWizardState>) {
  const [state, setState] = useState<QuoteWizardState>({
    step: 0,
    data: {},
    ...initialState,
  });

  const setStep = useCallback((step: number) => {
    setState(prev => ({ ...prev, step }));
  }, []);

  const setData = useCallback((data: Record<string, unknown>) => {
    setState(prev => ({ ...prev, data: { ...prev.data, ...data } }));
  }, []);

  const reset = useCallback(() => {
    setState({ step: 0, data: {}, ...initialState });
  }, [initialState]);

  return { state, setStep, setData, reset };
}
```

### 4. Effect Hook (Side Effects)

```bash
spaceos generate hook WindowResize --type effect
```

**Generált kód:**
```typescript
import { useEffect, useState } from 'react';

export function useWindowResize() {
  const [size, setSize] = useState({
    width: typeof window !== 'undefined' ? window.innerWidth : 0,
    height: typeof window !== 'undefined' ? window.innerHeight : 0,
  });

  useEffect(() => {
    const handleResize = () => {
      setSize({
        width: window.innerWidth,
        height: window.innerHeight,
      });
    };

    window.addEventListener('resize', handleResize);

    // Cleanup
    return () => {
      window.removeEventListener('resize', handleResize);
    };
  }, []);

  return size;
}
```

## Paraméterek

| Paraméter | Kötelező | Leírás |
|-----------|----------|--------|
| `name` | Igen | Hook neve (pl. Quotes, SubmitQuote) |
| `--type` | Igen | `query`, `mutation`, `state`, `effect` |
| `--with-test` | Nem | Generáljon tesztet is |
| `--with-cache` | Nem | TanStack Query cache használat |
| `--endpoint` | Nem | API endpoint (query/mutation esetén) |

## Generált Fájlok

```
client/src/hooks/
  └── useQuotes/
      ├── useQuotes.ts       # Fő hook
      ├── useQuotes.test.ts  # Teszt (--with-test)
      └── index.ts           # Export
```

## Frontend Terminal Használat

```bash
# Új query hook
spaceos generate hook Orders --type query --with-cache --endpoint /api/orders

# Új mutation hook
spaceos generate hook CreateOrder --type mutation --with-cache --endpoint /api/orders

# State management hook
spaceos generate hook CartState --type state

# Effect hook
spaceos generate hook DocumentTitle --type effect --with-test
```

## Referencia

- **ADR-050:** `/opt/spaceos/docs/architecture/decisions/ADR-050-codegen-toolchain.md`
- **Script:** `/opt/spaceos/scripts/codegen/generate-hook.sh`
- **MCP Tool:** `generate_hook` (spaceos-nexus/knowledge-service)
- **Dokumentáció:** `/opt/spaceos/docs/knowledge/patterns/CODEGEN_TOOLCHAIN_PATTERN.md`
