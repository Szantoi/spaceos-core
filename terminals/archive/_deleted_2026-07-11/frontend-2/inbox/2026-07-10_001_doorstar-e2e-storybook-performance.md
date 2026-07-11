---
id: MSG-FRONTEND2-001
from: conductor
to: frontend-2
type: task
priority: medium
status: UNREAD
model: sonnet
ref: MSG-FRONTEND-107
epic_id: EPIC-DOORSTAR-SOFTLAUNCH
estimated_nwt: 240
created: 2026-07-10
content_hash: 75229800281d9bc00187f78978e77a9d620553580c3150ba6f2726f1dfe2d43b
---

# Doorstar Production UI — E2E Tests, Storybook & Performance Optimization

**Epic:** EPIC-DOORSTAR-SOFTLAUNCH
**Priority:** MEDIUM (parallel track, quality assurance)
**Estimated:** 240 NWT (~2 days)
**Main Track:** MSG-FRONTEND-107 (DONE 2026-07-10, UI implementation complete)

---

## 🎯 OBJECTIVE

**Ensure production-ready quality** for Doorstar Production UI while Backend (MSG-BACKEND-196) implements the API. Focus on:
1. **E2E Testing** (Playwright) — User flow validation
2. **Storybook** — Component documentation & visual testing
3. **Performance Optimization** — Bundle size, lazy loading, lighthouse score

**Strategy:** Build quality assurance infrastructure **in parallel** to Backend development, ready for integration testing (2026-07-15).

---

## 📋 SCOPE

### Phase 1: E2E Testing with Playwright (~120 NWT / 1 day)

**Test Scenarios (4 user flows):**

**Test 1: Műhelyvezető — Production Queue Filtering**
```typescript
// e2e/production-queue.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Production Queue Page', () => {
  test('should filter by status (Folyamatban)', async ({ page }) => {
    // Arrange
    await page.goto('/production/jobs');
    await page.waitForLoadState('networkidle');

    // Act
    await page.click('button:has-text("Folyamatban")');

    // Assert
    const cards = await page.locator('[data-testid="production-job-card"]').all();
    expect(cards.length).toBeGreaterThan(0);

    // Verify all cards have "InProgress" status (yellow circles)
    for (const card of cards) {
      const statusCircles = await card.locator('[data-testid="step-circle"][data-status="InProgress"]').count();
      expect(statusCircles).toBeGreaterThan(0);
    }
  });

  test('should highlight overdue projects (red border)', async ({ page }) => {
    // Arrange
    await page.goto('/production/jobs');

    // Act
    await page.check('input[type="checkbox"]:has-text("Csak késésben lévők")');

    // Assert
    const overdueCards = await page.locator('[data-testid="production-job-card"][data-overdue="true"]').all();
    expect(overdueCards.length).toBeGreaterThan(0);

    for (const card of overdueCards) {
      const redBorder = await card.evaluate(el => getComputedStyle(el).borderColor);
      expect(redBorder).toContain('rgb(239, 68, 68)'); // Tailwind red-500
    }
  });

  test('should navigate to detail page on card tap', async ({ page }) => {
    // Arrange
    await page.goto('/production/jobs');

    // Act
    const firstCard = page.locator('[data-testid="production-job-card"]').first();
    const jobId = await firstCard.getAttribute('data-job-id');
    await firstCard.click();

    // Assert
    await expect(page).toHaveURL(`/production/jobs/${jobId}`);
    await expect(page.locator('h1')).toContainText('Munkamenet részletei');
  });
});
```

**Test 2: Műhelyvezető — Workflow Step Stepper (6 STAGE)**
```typescript
// e2e/workflow-stepper.spec.ts
test.describe('Workflow Step Stepper', () => {
  test('should complete step with optimistic UI', async ({ page }) => {
    // Arrange
    await page.goto('/production/jobs/123'); // Mock job ID
    await page.waitForLoadState('networkidle');

    // Act - Start "Szabászat" step
    const szabaszatStep = page.locator('[data-testid="workflow-step"][data-step-name="Szabászat"]');
    await szabaszatStep.locator('button:has-text("Start")').click();

    // Assert - Step status changes to InProgress (yellow)
    await expect(szabaszatStep).toHaveAttribute('data-status', 'InProgress');
    await expect(szabaszatStep).toHaveCSS('background-color', 'rgb(254, 240, 138)'); // yellow-200

    // Act - Complete "Szabászat" step
    await szabaszatStep.locator('button:has-text("Done")').click();

    // Assert - Step status changes to Done (green) immediately (optimistic UI)
    await expect(szabaszatStep).toHaveAttribute('data-status', 'Done');
    await expect(szabaszatStep.locator('[data-testid="step-circle"]')).toHaveCSS('background-color', 'rgb(34, 197, 94)'); // green-500
  });

  test('should require photo upload for "Összeszerelés" step', async ({ page }) => {
    // Arrange
    await page.goto('/production/jobs/123');
    await completeStepsUpTo(page, 'Felületkezelés'); // Helper: complete first 3 steps

    // Act - Try to complete "Összeszerelés" without photo
    const osszeszerelesStep = page.locator('[data-testid="workflow-step"][data-step-name="Összeszerelés"]');
    await osszeszerelesStep.locator('button:has-text("Done")').click();

    // Assert - Error message displayed
    await expect(page.locator('[role="alert"]')).toContainText('Fotó feltöltése kötelező');
    await expect(osszeszerelesStep).toHaveAttribute('data-status', 'InProgress'); // Still in progress
  });

  test('should upload photo for "Összeszerelés" step', async ({ page }) => {
    // Arrange
    await page.goto('/production/jobs/123');
    await completeStepsUpTo(page, 'Felületkezelés');

    // Act - Upload photo
    const osszeszerelesStep = page.locator('[data-testid="workflow-step"][data-step-name="Összeszerelés"]');
    const fileInput = osszeszerelesStep.locator('input[type="file"]');
    await fileInput.setInputFiles('./fixtures/test-photo.jpg');

    // Assert - Photo preview displayed
    await expect(osszeszerelesStep.locator('[data-testid="photo-preview"]')).toBeVisible();

    // Act - Complete step
    await osszeszerelesStep.locator('button:has-text("Done")').click();

    // Assert - Step completed
    await expect(osszeszerelesStep).toHaveAttribute('data-status', 'Done');
  });
});
```

**Test 3: SSE Real-Time Updates**
```typescript
// e2e/sse-realtime.spec.ts
test.describe('SSE Real-Time Updates', () => {
  test('should update UI on WorkflowStepCompleted event', async ({ page, context }) => {
    // Arrange
    await page.goto('/production/jobs/123');

    // Mock SSE event from Backend
    await context.route('/api/sse/production', route => {
      route.fulfill({
        status: 200,
        headers: { 'Content-Type': 'text/event-stream' },
        body: `data: {"type":"WorkflowStepCompleted","jobId":"123","stepName":"Szabászat"}\n\n`
      });
    });

    // Act - Wait for SSE event
    await page.waitForTimeout(1000);

    // Assert - UI updates automatically (cache invalidation)
    const szabaszatStep = page.locator('[data-testid="workflow-step"][data-step-name="Szabászat"]');
    await expect(szabaszatStep).toHaveAttribute('data-status', 'Done');
  });

  test('should show notification on ProductionJobShippingReady event', async ({ page, context }) => {
    // Arrange
    await page.goto('/production/overview');

    // Mock SSE event
    await context.route('/api/sse/production', route => {
      route.fulfill({
        status: 200,
        headers: { 'Content-Type': 'text/event-stream' },
        body: `data: {"type":"ProductionJobShippingReady","jobId":"123"}\n\n`
      });
    });

    // Act
    await page.waitForTimeout(1000);

    // Assert - Notification toast displayed
    await expect(page.locator('[role="alert"]')).toContainText('Kiszállítható');
  });
});
```

**Test 4: Mobile Kiosk — Touch Gestures**
```typescript
// e2e/mobile-kiosk.spec.ts
test.describe('Mobile Kiosk Layout', () => {
  test.use({ viewport: { width: 375, height: 667 } }); // iPhone SE

  test('should support touch tap on production cards', async ({ page }) => {
    // Arrange
    await page.goto('/production/jobs');

    // Act - Tap (touch event)
    const firstCard = page.locator('[data-testid="production-job-card"]').first();
    await firstCard.tap();

    // Assert - Navigation works
    await expect(page).toHaveURL(/\/production\/jobs\/\d+/);
  });

  test('should have minimum 48px tap targets (WCAG 2.1 AA)', async ({ page }) => {
    // Arrange
    await page.goto('/production/jobs/123');

    // Act
    const startButton = page.locator('button:has-text("Start")').first();
    const doneButton = page.locator('button:has-text("Done")').first();

    // Assert
    const startSize = await startButton.boundingBox();
    const doneSize = await doneButton.boundingBox();

    expect(startSize?.height).toBeGreaterThanOrEqual(48);
    expect(doneSize?.height).toBeGreaterThanOrEqual(48);
  });
});
```

**E2E Test Infrastructure:**
```typescript
// playwright.config.ts
export default defineConfig({
  testDir: './e2e',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'Desktop Chrome', use: { ...devices['Desktop Chrome'] } },
    { name: 'Mobile Safari', use: { ...devices['iPhone 12'] } },
  ],
});
```

---

### Phase 2: Storybook Component Documentation (~60 NWT / 4 hours)

**Storybook Stories (3 components):**

**Story 1: ProductionJobCard**
```typescript
// stories/ProductionJobCard.stories.tsx
import type { Meta, StoryObj } from '@storybook/react';
import { ProductionJobCard } from '../components/ProductionJobCard';

const meta: Meta<typeof ProductionJobCard> = {
  title: 'Production/ProductionJobCard',
  component: ProductionJobCard,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof ProductionJobCard>;

export const Queued: Story = {
  args: {
    job: {
      jobId: '123',
      orderId: '456',
      customerName: 'Kovács Kft.',
      deadline: new Date('2026-07-20'),
      status: 'Queued',
      steps: [
        { name: 'Szabászat', status: 'Pending' },
        { name: 'Megmunkálás', status: 'Pending' },
        // ... all 6 steps
      ],
      isOverdue: false,
    },
  },
};

export const InProgress: Story = {
  args: {
    job: {
      ...Queued.args.job,
      status: 'InProgress',
      steps: [
        { name: 'Szabászat', status: 'Done' },
        { name: 'Megmunkálás', status: 'InProgress' },
        // ...
      ],
    },
  },
};

export const Overdue: Story = {
  args: {
    job: {
      ...Queued.args.job,
      deadline: new Date('2026-07-01'), // Past date
      isOverdue: true,
    },
  },
};

export const ShippingReady: Story = {
  args: {
    job: {
      ...Queued.args.job,
      status: 'ShippingReady',
      steps: [
        { name: 'Szabászat', status: 'Done' },
        { name: 'Megmunkálás', status: 'Done' },
        // ... all Done
      ],
    },
  },
};
```

**Story 2: WorkflowStepStepper**
```typescript
// stories/WorkflowStepStepper.stories.tsx
export const Step1Pending: Story = {
  args: {
    steps: [
      { name: 'Szabászat', status: 'Pending', startedAt: null, completedAt: null },
      // ... rest pending
    ],
  },
};

export const Step3InProgress: Story = {
  args: {
    steps: [
      { name: 'Szabászat', status: 'Done', completedAt: '2026-07-10T10:00:00Z' },
      { name: 'Megmunkálás', status: 'Done', completedAt: '2026-07-10T11:00:00Z' },
      { name: 'Felületkezelés', status: 'InProgress', startedAt: '2026-07-10T12:00:00Z' },
      // ... rest pending
    ],
  },
};

export const AllStepsCompleted: Story = {
  args: {
    steps: [
      { name: 'Szabászat', status: 'Done', completedAt: '2026-07-10T10:00:00Z' },
      // ... all Done
    ],
  },
};
```

**Story 3: KioskMobileLayout**
```typescript
// stories/KioskMobileLayout.stories.tsx
export const WithBackButton: Story = {
  args: {
    title: 'Munkamenet részletei',
    onBack: () => alert('Back clicked'),
    children: <div>Content goes here</div>,
  },
};

export const FullScreenMode: Story = {
  parameters: {
    viewport: {
      defaultViewport: 'iphone12',
    },
  },
  args: {
    title: 'Kiosk Mode',
    children: <ProductionQueuePage />,
  },
};
```

**Storybook Config:**
```typescript
// .storybook/main.ts
export default {
  stories: ['../src/**/*.stories.@(js|jsx|ts|tsx)'],
  addons: [
    '@storybook/addon-essentials',
    '@storybook/addon-interactions',
    '@storybook/addon-a11y', // Accessibility testing
  ],
  framework: '@storybook/react-vite',
};
```

---

### Phase 3: Performance Optimization (~60 NWT / 4 hours)

**1. Bundle Size Analysis**
```bash
# Generate bundle analysis
npm run build -- --stats
npx vite-bundle-visualizer

# Check bundle size
ls -lh dist/assets/*.js
```

**Target:** Main bundle <500 KB (gzipped)

**Optimization 1: Code Splitting (Lazy Loading)**
```typescript
// App.tsx — Before
import ProductionQueuePage from './pages/ProductionQueuePage';
import ProductionJobDetailPage from './pages/ProductionJobDetailPage';

// After (lazy loading)
const ProductionQueuePage = lazy(() => import('./pages/ProductionQueuePage'));
const ProductionJobDetailPage = lazy(() => import('./pages/ProductionJobDetailPage'));
const ProductionOverviewPage = lazy(() => import('./pages/ProductionOverviewPage'));

// Wrap routes in Suspense
<Suspense fallback={<div>Loading...</div>}>
  <Routes>
    <Route path="/production/jobs" element={<ProductionQueuePage />} />
    <Route path="/production/jobs/:jobId" element={<ProductionJobDetailPage />} />
    <Route path="/production/overview" element={<ProductionOverviewPage />} />
  </Routes>
</Suspense>
```

**Optimization 2: Image Lazy Loading**
```typescript
// ProductionJobCard.tsx
<img
  src={photoUrl}
  alt="Production step photo"
  loading="lazy" // Native lazy loading
  decoding="async"
/>
```

**Optimization 3: React Query Devtools Tree-Shaking**
```typescript
// Only include devtools in development
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';

{process.env.NODE_ENV === 'development' && <ReactQueryDevtools />}
```

**2. Lighthouse Audit**
```bash
# Run Lighthouse (Performance, Accessibility, Best Practices)
npx lighthouse http://localhost:5173/production/jobs --view
```

**Target Scores:**
- Performance: ≥90
- Accessibility: ≥95
- Best Practices: ≥90

**3. TanStack Query Optimization**
```typescript
// useProductionQueue.ts — Add staleTime
export function useProductionQueue(filter: ProductionFilter) {
  return useQuery({
    queryKey: ['production', 'queue', filter],
    queryFn: () => fetchProductionQueue(filter),
    staleTime: 30_000, // 30s (already configured)
    gcTime: 5 * 60_000, // 5 min garbage collection
    refetchOnWindowFocus: true,
  });
}
```

**4. CSS Module Optimization**
```css
/* ProductionJobCard.module.css — Use CSS containment */
.card {
  contain: layout style paint; /* Optimize rendering */
}
```

---

## ✅ ACCEPTANCE CRITERIA

### E2E Testing
- [ ] 4 test scenarios implemented (Queue filtering, Stepper, SSE, Mobile)
- [ ] Playwright configured (Desktop + Mobile viewports)
- [ ] All tests GREEN (with mock API)
- [ ] Test coverage report generated

### Storybook
- [ ] 3 component stories created (ProductionJobCard, WorkflowStepStepper, KioskMobileLayout)
- [ ] All component states documented (Queued, InProgress, Overdue, ShippingReady)
- [ ] Accessibility addon enabled
- [ ] Storybook builds successfully

### Performance
- [ ] Bundle size analysis generated
- [ ] Lazy loading implemented (production routes)
- [ ] Lighthouse audit run (Performance ≥90, Accessibility ≥95)
- [ ] TanStack Query optimized (staleTime, gcTime)

---

## 📁 PROJECT STRUCTURE

```
datahaven-web/client/
├── e2e/
│   ├── production-queue.spec.ts          # E2E test 1
│   ├── workflow-stepper.spec.ts          # E2E test 2
│   ├── sse-realtime.spec.ts              # E2E test 3
│   ├── mobile-kiosk.spec.ts              # E2E test 4
│   └── fixtures/
│       └── test-photo.jpg                # Test photo for upload
├── .storybook/
│   ├── main.ts                           # Storybook config
│   └── preview.ts                        # Global decorators
├── src/stories/
│   ├── ProductionJobCard.stories.tsx
│   ├── WorkflowStepStepper.stories.tsx
│   └── KioskMobileLayout.stories.tsx
├── playwright.config.ts                  # Playwright config
└── vite.config.ts                        # Performance optimization
```

---

## 🚀 IMPLEMENTATION PLAN

### Day 1: E2E Testing (~8 hours)
- [ ] Install Playwright (`npm install -D @playwright/test`)
- [ ] Configure Playwright (Desktop + Mobile viewports)
- [ ] Implement Test 1 (Production Queue filtering)
- [ ] Implement Test 2 (Workflow Stepper)
- [ ] Implement Test 3 (SSE real-time)
- [ ] Implement Test 4 (Mobile kiosk)
- [ ] Run tests → Verify GREEN (with mock API)

### Day 2: Storybook + Performance (~8 hours)
- [ ] Install Storybook (`npx storybook@latest init`)
- [ ] Create 3 component stories
- [ ] Enable accessibility addon
- [ ] Run bundle size analysis
- [ ] Implement lazy loading (production routes)
- [ ] Run Lighthouse audit
- [ ] Optimize TanStack Query settings

**ETA:** 2026-07-12 EOD (2 days, parallel to Backend)

---

## 🔗 INTEGRATION WITH BACKEND

### Mock API → Real API Swap (2026-07-15)

**When Backend completes (MSG-BACKEND-196, 2026-07-14):**
1. Update E2E tests to use real API endpoints
2. Remove SSE mock routes
3. Run E2E tests against dev environment
4. Verify all tests GREEN

**Playwright Config Update:**
```diff
- baseURL: 'http://localhost:5173', // Mock API
+ baseURL: 'https://dev.spaceos.hu', // Real API
```

---

## 📊 SUCCESS METRICS

### E2E Testing
- [ ] 4 test scenarios GREEN (with mock API)
- [ ] Test coverage ≥80% (user flows)
- [ ] Test execution time <5 minutes

### Storybook
- [ ] 3 components documented
- [ ] All component states visible
- [ ] Accessibility checks pass

### Performance
- [ ] Main bundle <500 KB (gzipped)
- [ ] Lighthouse Performance ≥90
- [ ] Lighthouse Accessibility ≥95

---

## 📖 REFERENCES

| Document | Location |
|----------|----------|
| Frontend UI DONE | MSG-FRONTEND-107 (2026-07-10, 15 files) |
| Components | `datahaven-web/client/src/components/` |
| Pages | `datahaven-web/client/src/pages/` |
| Hooks | `datahaven-web/client/src/hooks/` |
| Backend API Spec | MSG-BACKEND-194 (OpenAPI contract) |
| Designer UX Audit | MSG-DESIGNER-002 (2026-07-10) |

---

## 🎯 DELIVERABLE

**DONE Message (2026-07-12):**
- E2E tests ready (4 scenarios, GREEN with mock API)
- Storybook stories published (3 components)
- Performance optimized (bundle size, Lighthouse score)
- Ready for Backend integration (2026-07-15)

---

📋 Conductor — MSG-FRONTEND2-001 Task Assignment (2026-07-10)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
