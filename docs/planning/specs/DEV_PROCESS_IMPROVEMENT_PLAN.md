# SpaceOS — Fejlesztési Folyamat Javítási Terv

> **Verzió:** 1.0
> **Létrehozva:** 2026-07-10
> **Státusz:** AKTÍV
> **Felelős:** Root → Nexus terminál

---

## Executive Summary

12 fejlesztési folyamat javítást azonosítottunk a terminál csapat hatékonyságának növelésére. A terv 3 fázisban valósul meg, prioritás szerint.

**Becsült ROI:**
- **Phase 1 (HIGH):** 30-40% gyorsabb feedback loop
- **Phase 2 (MEDIUM):** 20-30% kevesebb manuális munka
- **Phase 3 (LOW):** 10-15% jobb developer experience

---

## Phase 1: HIGH PRIORITY (Azonnali hatás)

### 1.1 Code Review Automation

**Probléma:**
- Manuális review bottleneck
- reviewer.sh néha lassú (Haiku 404 issue volt)
- Nincs pre-review validation

**Megoldás:**

```
PR/Commit
    ↓
┌─────────────────────────────────────┐
│  Pre-Review Gate (automated)        │
│  ├── ESLint/Prettier check          │
│  ├── TypeScript type check          │
│  ├── Unit test run                  │
│  ├── Security scan (npm audit)      │
│  └── Bundle size check              │
└─────────────────────────────────────┘
    ↓ (all green)
┌─────────────────────────────────────┐
│  AI Review Summary                  │
│  ├── Code diff analysis             │
│  ├── Pattern compliance check       │
│  ├── Potential issues highlight     │
│  └── Suggested improvements         │
└─────────────────────────────────────┘
    ↓
Human Review (reduced scope)
```

**Implementáció:**
1. `pre-review.sh` script létrehozása
2. GitHub Actions workflow (vagy local pre-commit hook)
3. AI summary generator (Claude API hívás)
4. Integration a reviewer.sh-val

**Fájlok:**
- `scripts/pre-review.sh`
- `spaceos-nexus/knowledge-service/src/pipeline/preReviewGate.ts`
- `.github/workflows/pre-review.yml` (opcionális)

**Acceptance Criteria:**
- [ ] Pre-review gate fut minden DONE outbox előtt
- [ ] ESLint + TypeScript + test check automatikus
- [ ] AI summary generálva (max 500 token)
- [ ] Review idő 50%-kal csökken

**Becsült effort:** 1-2 session
**ROI:** Review idő 50% csökkenés

---

### 1.2 Build Cache / Incremental Build

**Probléma:**
- Minden build full rebuild
- .NET restore minden alkalommal
- npm install lassú
- Docker build nem használ layer cache-t

**Megoldás:**

```
BUILD CACHE ARCHITECTURE
========================

.NET Backend:
├── NuGet package cache (~/.nuget/packages)
├── obj/ incremental compilation
├── Docker multi-stage with --mount=type=cache
└── dotnet build --no-restore (ha packages unchanged)

Node.js/React:
├── npm ci --cache ~/.npm
├── node_modules cache (hash-based)
├── Vite build cache (.vite/)
└── Turborepo remote cache (opcionális)

Docker:
├── Layer cache (COPY package*.json first)
├── BuildKit cache mounts
└── Registry cache (--cache-from)
```

**Implementáció:**
1. `.NET` — `dotnet build` optimization
2. `npm` — lockfile-based cache
3. `Docker` — BuildKit cache mounts
4. `Makefile` — unified build commands

**Fájlok:**
- `Makefile` (új vagy bővített)
- `Dockerfile` updates (cache mounts)
- `scripts/build-cached.sh`

**Acceptance Criteria:**
- [ ] Incremental .NET build < 10s (vs 30s full)
- [ ] npm install cache hit > 90%
- [ ] Docker build 50% gyorsabb (layer cache)
- [ ] `make build-fast` parancs működik

**Becsült effort:** 1 session
**ROI:** Build idő 50-70% csökkenés

---

### 1.3 Parallel Test Execution

**Probléma:**
- 278 teszt szekvenciálisan fut
- Backend tesztek: ~2-3 perc
- Frontend tesztek: ~1 perc
- Testcontainers: sequential startup

**Megoldás:**

```
PARALLEL TEST ARCHITECTURE
==========================

Backend (.NET xUnit):
├── [assembly: CollectionBehavior(MaxParallelThreads = 4)]
├── Testcontainers pool (shared PostgreSQL)
├── Test sharding (--filter Category=Unit)
└── Parallel test classes

Frontend (Vitest):
├── --pool=threads --poolOptions.threads.maxThreads=4
├── Test isolation (no shared state)
├── Component test parallelization
└── E2E: Playwright --workers=3

CI Pipeline:
├── Matrix strategy (backend || frontend)
├── Test result aggregation
└── Fail-fast on critical tests
```

**Implementáció:**
1. xUnit parallel configuration
2. Vitest worker config
3. Testcontainers connection pooling
4. CI matrix workflow

**Fájlok:**
- `spaceos-kernel/tests/xunit.runner.json`
- `datahaven-web/client/vitest.config.ts`
- `spaceos-nexus/knowledge-service/src/__tests__/setup/testcontainers-pool.ts`
- `.github/workflows/test-parallel.yml` (opcionális)

**Acceptance Criteria:**
- [ ] Backend tesztek < 1 perc (vs 2-3 perc)
- [ ] Frontend tesztek < 30s (vs 1 perc)
- [ ] Testcontainers pool működik (1 container, N connection)
- [ ] CI pipeline parallel matrix

**Becsült effort:** 1-2 session
**ROI:** Test idő 60-70% csökkenés

---

## Phase 2: MEDIUM PRIORITY (Hatékonyság növelés)

### 2.1 Hot Reload Development

**Probléma:** Backend restart minden változásnál

**Megoldás:**
- `dotnet watch run` integration
- Vite HMR already works
- Unified `make dev` command

**Fájlok:**
- `Makefile` (dev targets)
- `docker-compose.dev.yml`

**Becsült effort:** 0.5 session

---

### 2.2 Unified Dev Environment

**Probléma:** Terminálok különböző env-eket használnak

**Megoldás:**
- Docker Compose dev stack
- `.env.development` template
- `make dev` egyetlen parancs

**Fájlok:**
- `docker-compose.dev.yml`
- `.env.development.template`
- `Makefile`

**Becsült effort:** 1 session

---

### 2.3 Dependency Update Automation

**Probléma:** npm/NuGet outdated packages manuális

**Megoldás:**
- Renovate bot configuration
- Weekly dependency PR-ok
- Auto-merge for patch versions

**Fájlok:**
- `renovate.json`
- `.github/workflows/dependency-update.yml`

**Becsült effort:** 0.5 session

---

### 2.4 Error Tracking Integration

**Probléma:** Hibák csak log-ból derülnek ki

**Megoldás:**
- Sentry integration (backend + frontend)
- Error aggregation dashboard
- Alert rules

**Fájlok:**
- `spaceos-kernel/src/Sentry.config.cs`
- `datahaven-web/client/src/sentry.ts`

**Becsült effort:** 1 session

---

### 2.5 API Mock Server

**Probléma:** Frontend blocked ha backend nincs kész

**Megoldás:**
- MSW (Mock Service Worker) setup
- OpenAPI-based mock generation
- Prism mock server (opcionális)

**Fájlok:**
- `datahaven-web/client/src/mocks/`
- `datahaven-web/client/src/mocks/handlers.ts`

**Becsült effort:** 1 session

---

## Phase 1.5: ARCHITECTURE AUDIT (Stratégiai)

### 1.4 Knowledge-Service Architecture Audit

**Probléma:**
- 99 MCP tool egyetlen processzben
- Nem tisztázott, van-e teljesítményvesztés
- Single point of failure kockázat

**Vizsgálandó:**
1. Teljesítmény baseline (memory, CPU, response time)
2. Monolith vs Microservices trade-off
3. Darabolási opciók elemzése

**Opciók:**

| Opció | Leírás | Komplexitás |
|-------|--------|-------------|
| **A: Marad Monolith** | Jelenlegi állapot | Alacsony |
| **B: 2-Split (API + Worker)** | Stateless API + Background worker | Közepes |
| **C: 3-Split (Domain-based)** | Core + Pipeline + Integration | Magas |

**Deliverables:**
- Performance audit report
- ADR-062 (Architecture Decision Record)
- Benchmark results
- Migration plan (ha szükséges)

**Task:** MSG-NEXUS-013
**Model:** Opus (komplex architektúra döntés)

---

## Phase 3: LOW PRIORITY (Nice to have)

### 3.1 Documentation Generator

- OpenAPI auto-generate from code
- TypeDoc for TypeScript
- Storybook for components

### 3.2 Performance Profiling

- Lighthouse CI integration
- k6 load test scripts
- .NET profiler setup

### 3.3 Feature Flags

- LaunchDarkly or Unleash
- Gradual rollout support
- A/B testing capability

### 3.4 Developer Onboarding Kit

- `scripts/setup-terminal.sh`
- Template CLAUDE.md
- First-day checklist

---

## Implementation Timeline

```
Week 1 (Phase 1 - HIGH):
├── Day 1-2: Code Review Automation (MSG-NEXUS-010)
├── Day 3-4: Build Cache Implementation (MSG-NEXUS-011)
└── Day 5: Parallel Test Execution (MSG-NEXUS-012)

Week 2-3 (Phase 2 - MEDIUM):
├── Hot Reload Development
├── Unified Dev Environment
├── Dependency Update Automation
├── Error Tracking Integration
└── API Mock Server

Week 4+ (Phase 3 - LOW):
├── Documentation Generator
├── Performance Profiling
├── Feature Flags
└── Developer Onboarding Kit
```

---

## Success Metrics

| Metric | Current | Target | Measurement |
|--------|---------|--------|-------------|
| **Build time** | ~30s | <10s | `time make build` |
| **Test time** | ~3 min | <1 min | `time npm test` |
| **Review time** | ~15 min | <5 min | Outbox timestamp diff |
| **Dev startup** | ~5 min | <1 min | `time make dev` |
| **Feedback loop** | ~10 min | <3 min | Code → Test result |

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Cache invalidation bugs | Build fails | Clear cache script, fallback to full build |
| Parallel test flakiness | False failures | Retry logic, test isolation |
| AI review hallucination | Bad suggestions | Human review remains mandatory |

---

## References

- `docs/agent-infrastructure/ISSUES.md` — Known issues
- `docs/knowledge/patterns/MCP_TOOLS_CATALOGUE.md` — Existing tools
- `docs/WORKFLOW.md` — Current workflow definition

---

**Document Status:** APPROVED
**Next Review:** 2026-07-17 (Phase 1 completion)
