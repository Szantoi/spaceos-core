---
id: MSG-NEXUS-012
from: root
to: nexus
type: task
priority: high
status: UNREAD
model: sonnet
created: 2026-07-10
content_hash: 45e8ffd484124910aa79f5ea447e514bac04d40e957adcd3d39b82877cf83f31
---

# Parallel Test Execution — xUnit + Vitest + Testcontainers

## Kontextus

A DEV_PROCESS_IMPROVEMENT_PLAN.md Phase 1.3 szerint implementáld a párhuzamos teszt futtatást.

**Referencia:** `docs/planning/specs/DEV_PROCESS_IMPROVEMENT_PLAN.md`

## Probléma

- 278 teszt szekvenciálisan fut
- Backend tesztek: ~2-3 perc
- Frontend tesztek: ~1 perc
- Testcontainers: sequential startup

## Feladat

### 1. Vitest Parallel Configuration

Frissítsd `datahaven-web/client/vitest.config.ts`:

```typescript
import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    // Parallel execution
    pool: 'threads',
    poolOptions: {
      threads: {
        maxThreads: 4,
        minThreads: 2,
      },
    },
    
    // Test isolation
    isolate: true,
    
    // Faster startup
    globals: true,
    environment: 'jsdom',
    
    // Coverage
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
    },
  },
});
```

### 2. Jest Parallel (ha van Jest)

Ha knowledge-service Jest-et használ:

```javascript
// jest.config.js
module.exports = {
  maxWorkers: '50%', // Use half of CPU cores
  testTimeout: 30000,
  
  // Test sharding for CI
  shard: process.env.CI ? {
    current: parseInt(process.env.SHARD_INDEX || '1'),
    total: parseInt(process.env.SHARD_TOTAL || '1'),
  } : undefined,
};
```

### 3. Testcontainers Pool (knowledge-service)

Hozz létre `src/__tests__/setup/testcontainers-pool.ts`:

```typescript
import { PostgreSqlContainer, StartedPostgreSqlContainer } from '@testcontainers/postgresql';

let container: StartedPostgreSqlContainer | null = null;

export async function getTestDatabase(): Promise<string> {
  if (!container) {
    container = await new PostgreSqlContainer('postgres:15-alpine')
      .withDatabase('spaceos_test')
      .withUsername('test')
      .withPassword('test')
      .withReuse() // Reuse container across tests
      .start();
  }
  return container.getConnectionUri();
}

export async function cleanupTestDatabase(): Promise<void> {
  if (container) {
    await container.stop();
    container = null;
  }
}
```

### 4. Test Script Updates

Frissítsd a package.json scripts-et:

```json
{
  "scripts": {
    "test": "vitest run --reporter=verbose",
    "test:parallel": "vitest run --pool=threads --reporter=dot",
    "test:unit": "vitest run --exclude='**/*.e2e.test.ts'",
    "test:e2e": "vitest run --include='**/*.e2e.test.ts'",
    "test:coverage": "vitest run --coverage"
  }
}
```

### 5. CI Matrix (opcionális)

Ha GitHub Actions van:

```yaml
# .github/workflows/test.yml
jobs:
  test:
    strategy:
      matrix:
        shard: [1, 2, 3, 4]
    steps:
      - run: npm test -- --shard=${{ matrix.shard }}/4
```

## Acceptance Criteria

- [ ] Vitest parallel config működik
- [ ] Frontend tesztek < 30s (vs 1 perc)
- [ ] Backend/knowledge-service tesztek < 1 perc
- [ ] Testcontainers reuse működik
- [ ] `npm run test:parallel` parancs

## Acceptance Criteria

- [ ] Vitest parallel config működik
- [ ] Frontend tesztek < 30s
- [ ] Knowledge-service tesztek < 1 perc
- [ ] Testcontainers reuse működik
- [ ] npm run test:parallel parancs
