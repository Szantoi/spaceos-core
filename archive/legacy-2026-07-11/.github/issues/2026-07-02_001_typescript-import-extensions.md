# TypeScript import extensions hibák az mcp.ts és codegen modulokban

**Dátum:** 2026-07-02
**Prioritás:** 🔴 Critical
**Komponens:** spaceos-nexus/knowledge-service
**Státusz:** Fixed (2026-07-02)

## Probléma

A knowledge-service nem tudott elindulni, mert hibás TypeScript import extension-ök voltak a kódban:

```typescript
// HIBÁS (src/mcp.ts:109)
} from './codegen/index.js';

// HIBÁS (src/codegen/index.ts:22)
} from './codegenEngine.js';
```

**Hibaüzenet:**
```
Error: Cannot find module './codegen/index.js'
Require stack:
- /opt/spaceos/spaceos-nexus/knowledge-service/src/mcp.ts
```

**Következmény:**
- Knowledge-service crash-elt induláskor
- 3456-os port nem hallgatott
- MCP API endpoints nem érhetők el
- Memory save endpoint 404-et adott (mert a szerver nem futott)

## Gyökérok

TypeScript-ben **nem kell** `.js` extension az import statement-ekben, mert:
1. TypeScript fordítja `.ts` → `.js` fájlokat
2. A TypeScript compiler kezeli az extension mapping-et
3. `.js` extension csak runtime-ban releváns, fordítási időben nem

## Javítás

```typescript
// HELYES (src/mcp.ts:109)
} from './codegen/index';

// HELYES (src/codegen/index.ts:22)
} from './codegenEngine';
```

**Commitok:**
1. Fix `src/mcp.ts` line 109
2. Fix `src/codegen/index.ts` line 22
3. Restart knowledge-service

## Megelőzés (Recommendations)

### 1. ESLint szabály hozzáadása

```json
// .eslintrc.json
{
  "rules": {
    "import/extensions": ["error", "never", {
      "ts": "never",
      "tsx": "never"
    }]
  }
}
```

### 2. Pre-commit hook

```bash
# .husky/pre-commit
#!/bin/sh
grep -rn "from.*\.js['\"]" src/ && echo "ERROR: .js extension in TypeScript imports" && exit 1
```

### 3. TypeScript build ellenőrzés CI-ban

```yaml
# .github/workflows/ci.yml
- name: Build TypeScript
  run: |
    cd spaceos-nexus/knowledge-service
    npm run build
    # Ha compile error van, a build fail-el
```

## Testing Checklist

- [x] Knowledge-service elindult hibátlanul
- [x] 3456-os port hallgat
- [x] Health endpoint válaszol: `GET /health`
- [x] Memory save endpoint működik: `POST /api/memories/save` → 200 OK
- [x] MCP API elérhető

## Related Issues

- Memory save 402/404 error → ez volt az alapvető ok
- Knowledge-service startup failures → ismétlődő probléma volt

## Felelős

Root terminal (2026-07-02 session)
