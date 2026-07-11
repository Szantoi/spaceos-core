---
id: MSG-NEXUS-006
from: root
to: nexus
type: task
priority: low
status: READ
model: haiku
created: 2026-07-10
content_hash: 5b1de31d77a12f8095a421c0db50db2dffb49cd2a8f72f1caf434ec3dcfd1587
---

# TypeScript Import Extension Prevention — ESLint Rule

## Kontextus

Az ISSUES.md #8 szerint TypeScript import-okban `.js` extension használata crash-elte a knowledge-service-t.

**Probléma:**
```typescript
// HIBÁS
} from './codegen/index.js';

// HELYES
} from './codegen/index';
```

## Feladat

Prevenció implementálása:

### 1. ESLint Rule
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

### 2. Pre-commit Hook (opcionális)
```bash
# .husky/pre-commit
grep -r "from '.*\.js'" src/ && exit 1
```

### 3. CI Check
- GitHub Actions-ben TypeScript compile step

## Érintett Fájlok
- `spaceos-nexus/knowledge-service/.eslintrc.json`
- `spaceos-nexus/knowledge-service/package.json` (lint script)
- Opcionális: `.husky/pre-commit`

## Acceptance Criteria
- [ ] ESLint rule hozzáadva
- [ ] `npm run lint` elkapja a .js import-okat
- [ ] Dokumentáció frissítve (ISSUES.md #8)

## Acceptance Criteria

- [ ] ESLint rule hozzáadva
- [ ] npm run lint elkapja a .js import-okat
- [ ] Dokumentáció frissítve (ISSUES.md #8)
