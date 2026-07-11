---
id: MSG-NEXUS-010
from: root
to: nexus
type: task
priority: high
status: PROCESSED
model: sonnet
created: 2026-07-10
content_hash: d41ebf3480b32cd71f80a98fab8ba09f5b3e5f99ac3a9743300b245171f65cb6
---

# Code Review Automation — Pre-Review Gate Implementation

## Kontextus

A DEV_PROCESS_IMPROVEMENT_PLAN.md Phase 1.1 szerint implementáld az automatikus pre-review gate-et.

**Referencia:** `docs/planning/specs/DEV_PROCESS_IMPROVEMENT_PLAN.md`

## Probléma

- Manuális review bottleneck
- Nincs pre-review validation
- reviewer.sh önmagában nem elég gyors

## Feladat

### 1. Pre-Review Gate Script

Hozz létre `scripts/pre-review.sh`:

```bash
#!/bin/bash
# Pre-review gate — runs before reviewer.sh

set -e

echo "🔍 Pre-Review Gate Starting..."

# 1. ESLint check (frontend)
echo "→ ESLint..."
cd /opt/spaceos/datahaven-web/client
npm run lint --silent || { echo "❌ ESLint failed"; exit 1; }

# 2. TypeScript check
echo "→ TypeScript..."
npm run typecheck --silent || { echo "❌ TypeScript failed"; exit 1; }

# 3. Unit tests (fast, no E2E)
echo "→ Unit tests..."
npm run test:unit --silent || { echo "❌ Tests failed"; exit 1; }

# 4. Security audit
echo "→ Security audit..."
npm audit --audit-level=high || echo "⚠️ Security warnings"

# 5. Bundle size check
echo "→ Bundle size..."
npm run build 2>&1 | grep -E "chunk|size" || true

echo "✅ Pre-Review Gate Passed"
```

### 2. TypeScript Integration

Hozz létre `spaceos-nexus/knowledge-service/src/pipeline/preReviewGate.ts`:

```typescript
interface PreReviewResult {
  passed: boolean;
  checks: {
    eslint: boolean;
    typescript: boolean;
    tests: boolean;
    security: boolean;
    bundleSize: { kb: number; warning: boolean };
  };
  summary: string;
  duration_ms: number;
}

export async function runPreReviewGate(
  project: 'datahaven-web' | 'knowledge-service'
): Promise<PreReviewResult> {
  // Implementation...
}
```

### 3. AI Review Summary (opcionális)

Ha idő engedi, adj hozzá AI summary-t:
- Diff analysis
- Pattern compliance
- Potential issues

### 4. Integration

- `reviewer.sh` hívja előbb a pre-review gate-et
- Ha pre-review FAIL → skip AI review, return early
- watchDone.ts integration

## Érintett Fájlok

- Új: `scripts/pre-review.sh`
- Új: `src/pipeline/preReviewGate.ts`
- Módosítandó: `scripts/reviewer.sh`
- Módosítandó: `src/pipeline/watchDone.ts`

## Acceptance Criteria

- [ ] `scripts/pre-review.sh` fut és zöld
- [ ] ESLint + TypeScript + test check automatikus
- [ ] Pre-review < 30s
- [ ] Integration a reviewer.sh-val
- [ ] Dokumentáció (ISSUES.md vagy README)

## Acceptance Criteria

- [ ] scripts/pre-review.sh fut és zöld
- [ ] ESLint + TypeScript + test check automatikus
- [ ] Pre-review < 30s
- [ ] Integration a reviewer.sh-val
- [ ] Dokumentáció frissítve
