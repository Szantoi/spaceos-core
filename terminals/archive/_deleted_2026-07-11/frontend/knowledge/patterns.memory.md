# Frontend — Patterns Memory

> **TTL:** 14 nap (WARM) | **Frissítve:** 2026-07-04

---

## UX Design Principles — Implementation Guide

**Teljes dokumentum:** `docs/knowledge/patterns/UX_DESIGN_PRINCIPLES.md`

### Mobile-First Implementation

```tsx
// Touch target minimum 44×44px
<button className="min-h-[44px] min-w-[44px] p-3">
  <Icon />
</button>

// Thumb zone - fontos CTA-k az alsó harmadban
<div className="fixed bottom-0 left-0 right-0 p-4">
  <Button variant="primary">Fő Akció</Button>
</div>
```

### Responsive Breakpoints

```css
/* Mobile first */
.component { /* mobil stílusok */ }

@media (min-width: 480px) { /* tablet portrait */ }
@media (min-width: 768px) { /* tablet landscape */ }
@media (min-width: 1200px) { /* desktop */ }
```

### Design Token Usage

```css
/* ✅ JÓ - Design token */
.card {
  background: var(--bg-secondary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
}

/* ❌ ROSSZ - Hard-coded érték */
.card {
  background: #1a1a2e;
  color: #ffffff;
}
```

### Dark Theme Color Tokens

```css
:root {
  --bg-primary: #0f1419;
  --bg-secondary: #1a1a2e;
  --text-primary: #e7e9ea;
  --text-secondary: #8b949e;
  --accent-blue: #1d9bf0;
  --status-success: #00ba7c;
  --status-warning: #ffd400;
  --status-error: #f4212e;
}
```

### Single-Screen Focus Pattern

```tsx
// Progresszív felfedés - részletek elrejtve
const [showDetails, setShowDetails] = useState(false);

return (
  <div>
    <Summary data={data} />
    <Button onClick={() => setShowDetails(!showDetails)}>
      {showDetails ? 'Kevesebb' : 'Részletek'}
    </Button>
    {showDetails && <Details data={data} />}
  </div>
);
```

---

## Checklist UI Implementációhoz

- [ ] Touch target ≥ 44×44px minden interaktív elemre
- [ ] Responsive: 480px, 768px, 1200px breakpointok
- [ ] Design tokenek (nincs hard-coded szín)
- [ ] Dark theme alapértelmezett
- [ ] Max 7±2 fő elem per képernyő

---

## Designer Koordináció

**Workflow:**
1. Designer → Design Spec → Frontend inbox
2. Frontend implementál
3. Designer → UI Review → APPROVED / REVISION
4. Ha REVISION → Javítás → Újra review

**Designer review kötelező** ha:
- Új UI komponens
- Meglévő UI jelentős változása
- Mobile layout változás

---

## Referenciák

- `docs/knowledge/patterns/UX_DESIGN_PRINCIPLES.md` — UX elvek
- `docs/knowledge/patterns/REACT_18_TYPESCRIPT_MODERNIZATION.md` — React patterns
- `docs/joinerytech/ui.jsx` — JoineryTech UI minták

---

## Contract-First Development — MSW Parallel Development Pattern

**Teljes dokumentum:** `docs/knowledge/patterns/CONTRACT_FIRST_DEVELOPMENT.md`

### Week 0 Workflow Summary

**Before coding starts:**
1. Architect + Backend + Frontend write OpenAPI 3.1 spec (3-4 days)
2. Endpoints, schemas, validation rules, error responses documented
3. Spec locked → code generation setup (Orval for Frontend)
4. Frontend can start development immediately with mock API

**ROI:** $4k investment → $11k-16k savings (prevents 2 weeks of rework)

### MSW Mock API Setup Checklist

```bash
# 1. Install MSW
npm install --save-dev msw

# 2. Generate handlers from OpenAPI spec
# (or write manually based on spec)

# 3. Create handlers file
```

**Example:** `src/mocks/handlers.ts`
```typescript
import { http, HttpResponse } from 'msw';

export const handlers = [
  http.post('/v1/auth/login', async ({ request }) => {
    const body = await request.json();

    // Mock validation (matches OpenAPI spec)
    if (!body.email || !body.password) {
      return HttpResponse.json(
        { message: 'Validation failed', code: 'VALIDATION_ERROR' },
        { status: 422 }
      );
    }

    // Mock success response (matches OpenAPI spec)
    return HttpResponse.json({
      accessToken: 'mock-jwt-token-12345',
      expiresIn: 3600,
    });
  }),
];
```

**Enable in dev:** `src/main.tsx`
```typescript
if (import.meta.env.DEV) {
  const { worker } = await import('./mocks/browser');
  worker.start();
}
```

### Orval Code Generation Setup

**Config:** `orval.config.ts`
```typescript
import { defineConfig } from 'orval';

export default defineConfig({
  api: {
    input: '../docs/joinerytech/API_SPEC_PHASE1.yaml',
    output: {
      mode: 'tags-split',
      target: 'src/api/generated',
      client: 'react-query',
      clean: true,
      override: {
        mutator: {
          path: './src/api/custom-instance.ts',
          name: 'customInstance',
        },
      },
    },
  },
});
```

**Generate:**
```bash
npx orval
```

**Usage:**
```typescript
import { useLogin } from '@/api/generated/authentication';

function LoginForm() {
  const { mutate: login, isPending, error } = useLogin();

  const handleSubmit = (email: string, password: string) => {
    login(
      { data: { email, password } },
      {
        onSuccess: (data) => console.log('Token:', data.accessToken),
        onError: (error) => console.error('Login failed:', error.message),
      }
    );
  };
}
```

### Feature Flags for Real API Swap

**Environment variables:**
```bash
# .env.development (mock API)
VITE_USE_MOCK_API=true

# .env.production (real API)
VITE_USE_MOCK_API=false
```

**API client:** `src/api/custom-instance.ts`
```typescript
import axios from 'axios';

const baseURL = import.meta.env.VITE_USE_MOCK_API
  ? '' // MSW intercepts requests
  : import.meta.env.VITE_API_URL;

export const customInstance = axios.create({
  baseURL,
  withCredentials: true, // HttpOnly cookie
});
```

### When to Start MSW Development

**Checkpoint trigger:** Week 0 OpenAPI spec locked ✅

**Independence criteria:**
- OpenAPI spec reviewed and approved
- Code generation (Orval) setup working
- MSW handlers match spec responses
- Build: 0 TypeScript errors

**Timeline:**
- Week 0: Spec writing (3-4 days)
- Week 1-4: **Frontend parallel development with MSW** (Backend implements real API)
- Week 5: Feature flag swap → real API integration

**Key benefit:** 2-4 weeks earlier delivery (Frontend not blocked waiting for Backend)

---

## Review Redundancy Architecture — Dual-Reviewer Pattern

**Teljes dokumentum:** `docs/knowledge/patterns/REVIEW_REDUNDANCY_ARCHITECTURE.md`

### Pattern Overview

**Problem:** Single reviewer failure blocks entire pipeline
**Solution:** Parallel review by Architect + Librarian → at least 1 approval required

### Review Focus Separation

**Architect Review (Technical):**
- Code quality and architecture compliance
- 5 Golden Rules validation
- Module boundary integrity
- Test coverage thresholds

**Librarian Review (Knowledge):**
- Knowledge synthesis opportunities
- Documentation completeness
- Pattern reusability
- Memory tier promotion

**Both reviewers can APPROVE — either one sufficient.**

### Failure Scenarios

**Scenario 1: Architect session hangs**
- Librarian completes review → APPROVED
- Pipeline continues (no delay)

**Scenario 2: Both sessions fail**
- Conductor escalates to Root
- Root manual approval (emergency fallback)

**Scenario 3: Conflicting reviews**
- Architect APPROVES + Librarian REJECTS (docs missing)
- Pipeline continues (at least 1 approval)
- Conductor creates follow-up task for documentation

### Frontend Impact

**When does this matter for Frontend?**
- Frontend DONE messages also go through dual review
- If Architect review fails (tmux hang), Librarian approval sufficient
- No pipeline blockage → faster feedback loop

**Health metrics:**
- Dual review success rate: ≥95% (target: 98%)
- Single point of failure rate: ≤5% (actual: 1%)
- Avg review time: <15 min (actual: 8.2 min)

---

## Checkpoint Coordination Workflow

**Skill:** `.claude/skills/checkpoint-coordination-workflow/`

### Multi-Team Epic Coordination

**Use case:** Frontend needs Backend API before starting integration

**Checkpoint example:**
```yaml
# EPICS.yaml
epics:
  - id: EPIC-PORTAL-V2
    checkpoints:
      - id: API_READY
        description: "Backend Auth + Catalog API complete"
        depends_on: ["EPIC-KERNEL-AUTH"]
        triggers: ["Frontend can start MSW → real API swap"]
```

**Frontend trigger workflow:**
1. Subscribe to Backend checkpoint: `API_READY`
2. Backend completes DONE → Checkpoint notification
3. Frontend receives notification → Start real API integration
4. Week 5: Feature flag swap (`VITE_USE_MOCK_API=false`)

**When to check for checkpoints:**
- New epic starts → Check if depends_on any Backend checkpoints
- Backend DONE message arrives → Check if it's a checkpoint trigger
- Planning phase → Identify integration dependencies

---

## MSW Parallel Development Checklist

**Week 0 (Contract-First):**
- [ ] OpenAPI spec locked (Architect + Backend + Frontend reviewed)
- [ ] Orval config written (`orval.config.ts`)
- [ ] MSW installed (`npm install --save-dev msw`)
- [ ] Handlers scaffold created (`src/mocks/handlers.ts`)

**Week 1 (MSW Setup):**
- [ ] Code generation working (`npx orval` → 0 errors)
- [ ] Mock handlers match OpenAPI spec responses
- [ ] Dev server runs with MSW enabled
- [ ] Feature flag setup (`VITE_USE_MOCK_API=true`)

**Week 1-4 (Parallel Development):**
- [ ] Frontend UI components using auto-generated hooks
- [ ] TanStack Query caching working
- [ ] Error handling (400, 401, 422, 500) tested with MSW

**Week 5 (Integration):**
- [ ] Backend checkpoint: API_READY ✅
- [ ] Feature flag swap: `VITE_USE_MOCK_API=false`
- [ ] Integration test: real API calls work first time
- [ ] MSW disabled in production build

---

## Referenciák (Updated)

- `docs/knowledge/patterns/UX_DESIGN_PRINCIPLES.md` — UX elvek
- `docs/knowledge/patterns/REACT_18_TYPESCRIPT_MODERNIZATION.md` — React patterns
- `docs/joinerytech/ui.jsx` — JoineryTech UI minták
- `docs/knowledge/patterns/CONTRACT_FIRST_DEVELOPMENT.md` — **OpenAPI Week 0 workflow**
- `docs/knowledge/patterns/REVIEW_REDUNDANCY_ARCHITECTURE.md` — **Dual-reviewer pattern**
- `.claude/skills/mock-api-parallel-development/` — **MSW setup skill**
- `.claude/skills/contract-first-development-workflow/` — **Week 0 OpenAPI skill**
- `.claude/skills/checkpoint-coordination-workflow/` — **Epic coordination skill**
