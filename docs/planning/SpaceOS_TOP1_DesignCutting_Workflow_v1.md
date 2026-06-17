# SpaceOS — TOP 1: Design→Cutting Workflow Integration
## Frontend-only implementation · Backend endpoints ready

> Verzió: v1.0 — 2026-06-16
> Státusz: DRAFT
> Scope: Frontend integration only (backend deployed)
> Review: None yet → v1

---

## Összefoglaló

A **Design→Cutting Workflow** feature összköti a DesignPage (Step 4 submit) és a ProductionPage (cutting plan list) közötti üzleti folyamatot. Jelenleg a DesignPage mock `cuttingPlanId`-t generál és nem navigál, a ProductionPage pedig nem tudja kiemelni a frissen létrehozott tervet.

**Üzleti érték:** A tervező (designer) az ajtó tervezés végeztével **egy kattintással** továbbjuthat a gyártási nézetbe, ahol azonnal látja az általa létrehozott vágási tervet. Ez eliminálja a jelenlegi manuális workflow-t (tervező → üzemvezető emailen keresztül).

**Backend állapot:** ✅ KÉSZ
- `POST /cutting/api/sheets` (SubmitCuttingSheet) — 931/931 teszt, deployed
- `GET /cutting/api/plans` — lista endpoint deployed

**Frontend scope:**
- DesignPage Step 4: mock submit → real API call + navigation
- ProductionPage: URL state-based highlight + scroll

---

## 1. Kumulált Finding Összesítő (v1 → v1)

**v1 Draft:** Még nincs review, első verzió.

---

## 2. Feature scope

### 2.1 User story

```gherkin
Feature: Design to Cutting workflow integration
  As a designer
  I want to submit my cutting list and immediately see it in Production
  So that I don't need to manually notify the workshop supervisor

  Scenario: Submit cutting list from DesignPage
    Given I am on DesignPage Step 4
    And I have a valid cutting list (5+ parts)
    When I click "Terv létrehozása és tovább a Gyártásba"
    Then the system calls POST /cutting/api/sheets
    And I am navigated to /w/production/cutting
    And the newly created plan is highlighted
    And the plan row shows customer name + template name

  Scenario: Error handling on submit
    Given I am on DesignPage Step 4
    When the API returns 409 Conflict (duplicate)
    Then I see a toast: "Ez a terv már létezik"
    And I remain on DesignPage
    And I can retry with modified data
```

### 2.2 Out of scope

- Backend endpoint fejlesztés (már kész)
- Breadcrumb trail vagy history tracking (későbbre)
- Email notification (későbbre)
- Offline support (későbbre)

---

## 3. API felület (meglévő)

### 3.1 POST /cutting/api/sheets (SubmitCuttingSheet)

**Endpoint:** `POST /cutting/api/sheets`
**Auth:** Bearer JWT (RS256)
**RBAC:** `designer` vagy `production_manager` role

**Request:**
```typescript
{
  orderReference: string      // pl. "ORD-2024-001"
  templateId: string          // UUID (Abstractions template)
  calculatedParts: Array<{
    partId: string            // UUID
    catalogType: string       // "DOOR_PANEL", "FRAME_VERTICAL", stb.
    width: number             // mm
    height: number            // mm
    quantity: number
    material: string          // "Bükk", "Tölgy", "MDF", stb.
  }>
}
```

**Response 201:**
```typescript
{
  sheetId: string           // CuttingSheet UUID
  cuttingPlanId: string     // CuttingPlan UUID (displayed on ProductionPage)
}
```

**Response 400:**
```json
{
  "error": "ValidationFailed",
  "details": "calculatedParts must contain at least 1 item"
}
```

**Response 409:**
```json
{
  "error": "DuplicateCuttingSheet",
  "details": "A cutting sheet for order ORD-2024-001 already exists"
}
```

### 3.2 GET /cutting/api/plans (list)

**Endpoint:** `GET /cutting/api/plans?status=Draft&limit=50`
**Auth:** Bearer JWT (RS256)
**RBAC:** bármely autentikált user (tenant RLS filter)

**Response 200:**
```typescript
{
  plans: Array<{
    id: string                  // CuttingPlan UUID
    orderReference: string      // back-ref az eredeti rendeléshez
    templateName: string        // "Egyszárnyú ajtó", stb.
    customerName: string        // "Kovács Bt." (jelenleg üres — bővítés szükséges)
    status: "Draft" | "Planned" | "InProgress" | "Completed"
    createdAt: string           // ISO8601
    partsCount: number
  }>
}
```

**FONTOS:** Jelenleg a `customerName` üres — backend nem tölti ki. Ez a frontend részéről **tolerálható** (fallback: "—"), de a UX javításához backend módosítás kell (későbbi track).

---

## 4. Frontend implementation

### 4.1 DesignPage changes

**File:** `/opt/spaceos/frontend/joinerytech-portal/src/pages/DesignPage.tsx`

#### 4.1.1 Mock eltávolítása

**Jelenleg (lines ~279-285):**
```typescript
const handleSubmit = () => {
  const mockCuttingPlanId = `CP-${Date.now()}-MOCK`;
  console.log('Mock submit:', mockCuttingPlanId);
  // TODO: real API call
};
```

**Új implementáció:**
```typescript
import { useApi } from '../hooks/useApi';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-hot-toast';

const DesignPage = () => {
  const navigate = useNavigate();
  const { post, loading, error } = useApi();

  const handleSubmit = async () => {
    try {
      const response = await post<{ sheetId: string; cuttingPlanId: string }>(
        `${import.meta.env.VITE_CUTTING_API_BASE}/api/sheets`,
        {
          orderReference: currentOrderRef,
          templateId: selectedTemplate.id,
          calculatedParts: cuttingList.map(part => ({
            partId: part.id,
            catalogType: part.type,
            width: part.width,
            height: part.height,
            quantity: part.quantity,
            material: part.material
          }))
        }
      );

      toast.success('Vágási terv létrehozva!');
      navigate('/w/production/cutting', {
        state: { highlightPlanId: response.cuttingPlanId }
      });
    } catch (err) {
      if (err.status === 409) {
        toast.error('Ez a terv már létezik');
      } else if (err.status === 400) {
        toast.error('Érvénytelen adatok: ' + err.message);
      } else {
        toast.error('Hiba történt a terv létrehozása során');
      }
    }
  };

  // ...
};
```

#### 4.1.2 Button text update

**Jelenleg:**
```tsx
<button onClick={handleSubmit}>Terv létrehozása</button>
```

**Új:**
```tsx
<button onClick={handleSubmit} disabled={loading}>
  {loading ? 'Küldés...' : 'Terv létrehozása és tovább a Gyártásba'}
</button>
```

#### 4.1.3 Validation layer (pre-submit)

**Cél:** Biztosítani hogy a `cuttingList` formátum egyezik az API elvárásokkal.

```typescript
const validateCuttingList = (list: CuttingPart[]) => {
  if (list.length === 0) {
    throw new Error('A vágási lista nem lehet üres');
  }
  for (const part of list) {
    if (!part.width || part.width <= 0) {
      throw new Error(`Érvénytelen szélesség: ${part.id}`);
    }
    if (!part.height || part.height <= 0) {
      throw new Error(`Érvénytelen magasság: ${part.id}`);
    }
    if (!part.quantity || part.quantity <= 0) {
      throw new Error(`Érvénytelen mennyiség: ${part.id}`);
    }
  }
};

const handleSubmit = async () => {
  try {
    validateCuttingList(cuttingList);
    // ... API call
  } catch (err) {
    toast.error(err.message);
    return;
  }
};
```

### 4.2 ProductionPage changes

**File:** `/opt/spaceos/frontend/joinerytech-portal/src/pages/ProductionPage.tsx`

#### 4.2.1 URL state detection

```typescript
import { useLocation } from 'react-router-dom';
import { useEffect, useRef } from 'react';

const ProductionPage = () => {
  const location = useLocation();
  const highlightPlanId = location.state?.highlightPlanId;
  const highlightedRowRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (highlightPlanId) {
      setSelectedPlan(highlightPlanId);

      // Scroll to row
      setTimeout(() => {
        highlightedRowRef.current?.scrollIntoView({
          behavior: 'smooth',
          block: 'center'
        });
      }, 300); // Wait for render

      // Remove highlight after 3s
      setTimeout(() => {
        // Clear location state (optional)
        window.history.replaceState({}, document.title);
      }, 3000);
    }
  }, [highlightPlanId]);

  // ...
};
```

#### 4.2.2 Visual highlight (CSS)

**Plan row rendering:**
```tsx
{plans.map(plan => (
  <div
    key={plan.id}
    ref={plan.id === highlightPlanId ? highlightedRowRef : null}
    className={cn(
      'plan-row',
      plan.id === selectedPlan && 'selected',
      plan.id === highlightPlanId && 'animate-pulse border-2 border-teal-500'
    )}
    onClick={() => setSelectedPlan(plan.id)}
  >
    <div className="plan-header">
      <span className="plan-id">{plan.id}</span>
      <span className="customer-name">{plan.customerName || '—'}</span>
      <span className="template-name">{plan.templateName}</span>
    </div>
    {/* ... */}
  </div>
))}
```

**Tailwind animation (tailwind.config.js):**
```js
module.exports = {
  theme: {
    extend: {
      animation: {
        'pulse': 'pulse 1.5s cubic-bezier(0.4, 0, 0.6, 1) 2',
      }
    }
  }
}
```

#### 4.2.3 Customer name + template name display

**Jelenleg:** Plan sorban csak `plan.id` látszik.

**Új:** Hozzáadni `customerName` (fallback: "—") és `templateName` oszlopot.

**Backend függőség:** A `GET /cutting/api/plans` response-ban a `customerName` üres. Ez **tolerálható** (fallback működik), de későbbi backend bővítés javasolt:

**Backend task (későbbre):**
```
CUTTING modul: GetCuttingPlansQuery bővítése
- JOIN Order table → Customer aggregate
- Response DTO: customerName mező populálása
- Teszt: GetCuttingPlans_ReturnsCustomerName
```

---

## 5. Testing strategy

### 5.1 Unit tests (Vitest)

**DesignPage.test.tsx** (új tesztek):
```typescript
describe('DesignPage Step 4 submit', () => {
  it('should call POST /cutting/api/sheets with correct payload', async () => {
    const mockPost = vi.fn().mockResolvedValue({ cuttingPlanId: 'CP-001' });
    render(<DesignPage />, { mockApi: { post: mockPost } });

    await userEvent.click(screen.getByText('Terv létrehozása'));

    expect(mockPost).toHaveBeenCalledWith(
      '/cutting/api/sheets',
      expect.objectContaining({
        orderReference: expect.any(String),
        calculatedParts: expect.any(Array)
      })
    );
  });

  it('should navigate to /w/production/cutting on success', async () => {
    const mockNavigate = vi.fn();
    render(<DesignPage />, { mockNavigate });

    await userEvent.click(screen.getByText('Terv létrehozása'));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith(
        '/w/production/cutting',
        { state: { highlightPlanId: 'CP-001' } }
      );
    });
  });

  it('should show error toast on 409 Conflict', async () => {
    const mockPost = vi.fn().mockRejectedValue({ status: 409 });
    render(<DesignPage />, { mockApi: { post: mockPost } });

    await userEvent.click(screen.getByText('Terv létrehozása'));

    expect(screen.getByText('Ez a terv már létezik')).toBeInTheDocument();
  });
});
```

**ProductionPage.test.tsx** (új tesztek):
```typescript
describe('ProductionPage highlight', () => {
  it('should auto-select plan from location state', () => {
    render(<ProductionPage />, {
      initialEntries: [
        { pathname: '/w/production/cutting', state: { highlightPlanId: 'CP-001' } }
      ]
    });

    expect(screen.getByTestId('plan-CP-001')).toHaveClass('selected');
  });

  it('should scroll to highlighted plan', async () => {
    const scrollIntoViewMock = vi.fn();
    Element.prototype.scrollIntoView = scrollIntoViewMock;

    render(<ProductionPage />, {
      initialEntries: [
        { pathname: '/w/production/cutting', state: { highlightPlanId: 'CP-001' } }
      ]
    });

    await waitFor(() => {
      expect(scrollIntoViewMock).toHaveBeenCalledWith({
        behavior: 'smooth',
        block: 'center'
      });
    });
  });

  it('should display customer name or fallback', () => {
    const plans = [
      { id: 'CP-001', customerName: 'Kovács Bt.', templateName: 'Egyszárnyú' },
      { id: 'CP-002', customerName: '', templateName: 'Kétszárnyú' }
    ];

    render(<ProductionPage />, { mockData: { plans } });

    expect(screen.getByText('Kovács Bt.')).toBeInTheDocument();
    expect(screen.getByText('—')).toBeInTheDocument(); // fallback
  });
});
```

**Expected test count:**
- DesignPage: +3 teszt (submit flow, navigation, error handling)
- ProductionPage: +3 teszt (auto-select, scroll, customer name fallback)
- **Total:** +6 FE tests (247 → 253)

### 5.2 E2E test (Playwright — későbbre)

```typescript
test('Design to Cutting workflow', async ({ page }) => {
  await page.goto('/w/design');
  await page.fill('[data-testid="order-reference"]', 'ORD-E2E-001');
  await page.click('[data-testid="step-4-submit"]');

  await page.waitForURL('/w/production/cutting');
  await expect(page.locator('[data-highlight="true"]')).toBeVisible();
  await expect(page.locator('.customer-name')).toContainText('Kovács Bt.');
});
```

*(E2E test későbbi track — jelenleg unit tesztek elegendőek)*

---

## 6. Definition of Done

### Frontend

- [ ] DesignPage Step 4 submit hívja a `POST /cutting/api/sheets` endpoint-ot (mock kód eltávolítva)
- [ ] API response alapján navigation `/w/production/cutting` (location state: `highlightPlanId`)
- [ ] ProductionPage auto-select + scroll a highlighted plan-ra
- [ ] Plan sorban megjelenik `customerName` (fallback: "—") és `templateName`
- [ ] Error handling: 409 Conflict → toast "Ez a terv már létezik"
- [ ] Error handling: 400 Validation → toast "Érvénytelen adatok: {message}"
- [ ] Error handling: 5xx → toast "Hiba történt a terv létrehozása során"
- [ ] Button loading state (disabled + spinner míg API hívás fut)
- [ ] +6 FE unit teszt pass (DesignPage: 3, ProductionPage: 3)
- [ ] Teljes FE teszt suite: 253/253 pass
- [ ] `pnpm build` 0 error, 0 warning
- [ ] TypeScript strict mode: 0 type error
- [ ] Lint: 0 ESLint error

### Backend

- [ ] Nincs új backend munka (endpoint deployed ✅)

### Integration

- [ ] VPS deploy: `dist/` updated, nginx reload
- [ ] Smoke test: https://joinerytech.hu/w/design → Step 4 submit → ProductionPage highlight
- [ ] Manual QA: tesztelni 409 Conflict case-t (duplicate submit)

### Security

- [ ] JWT Bearer token továbbítása minden API hívásban
- [ ] CORS preflight működik (`POST /cutting/api/sheets` CORS header OK)
- [ ] No sensitive data in console.log (production build)

### Documentation

- [ ] Frontend CHANGELOG update: "FE-TOP1: Design→Cutting workflow integration"
- [ ] Codebase_Status.md frissítés: test count 247 → 253

---

## 7. Kockázatok és függőségek

### Kockázatok

| ID | Súly | Terület | Probléma | Javítás |
|---|---|---|---|---|
| R-01 | 🔴 CRITICAL | API contract | `cuttingList` formátum nem egyezik a backend DTO-val | Pre-implementation verification: olvasd el `SubmitCuttingSheetCommand.cs` DTO-t és hasonlítsd össze `DesignPage.tsx` state struktúrával |
| R-02 | 🟠 MEDIUM | UX | `customerName` üres backend response-ban → fallback UX | Fallback: "—" megjelenítése, backend bővítés későbbre ütemezve |
| R-03 | 🟡 LOW | Navigation | `navigate()` state nem perzisztens (page refresh → highlight elvész) | Elfogadható: highlight 3s-ig él, csak first load-ra kell |

### Függőségek

**Upstream (blokkoló):**
- Nincs blokkoló függőség — backend kész

**Downstream (ezt blokkolja):**
- TOP 2 (Nesting Visualization) — jobb UX ha ez kész (design→cutting link után azonnal látszik a nesting)

---

## 8. Roadmap

**Sprint 1 (2-3 nap):**
- [ ] Day 1: DesignPage submit API integráció + validation + unit tesztek
- [ ] Day 2: ProductionPage highlight + scroll + customer name display + unit tesztek
- [ ] Day 3: Error handling, loading state, lint/build gate, deploy

**Sprint 2 (későbbi bővítés):**
- [ ] Backend: `GET /cutting/api/plans` bővítése `customerName` populálással
- [ ] E2E test: Design→Cutting full workflow
- [ ] Breadcrumb trail (optional UX improvement)

---

## 9. Status Banner

```
┌────────────────────────────────────────────────────────────────┐
│ SpaceOS — TOP 1: Design→Cutting Workflow Integration          │
│ Verzió: v1.0 DRAFT — 2026-06-16                                │
│ Scope: Frontend-only (backend ✅ kész)                          │
│ Implementation: 2-3 nap FE · 0 nap BE                           │
│ Test delta: +6 FE unit tests (247 → 253)                       │
│ Critical risk: cuttingList format verification (R-01)          │
│ Next: v2 DB review (SKIP — nincs DB change)                    │
│        v3 Security review                                      │
└────────────────────────────────────────────────────────────────┘
```

---

## 10. Claude Code implementációs csomag

**Pre-implementation checklist:**
1. Verify `cuttingList` format vs. `SubmitCuttingSheetCommand.cs` DTO
2. Check `useApi()` hook exists in codebase
3. Check `react-hot-toast` package installed
4. Verify `VITE_CUTTING_API_BASE` env var configured

**Implementation order:**
1. DesignPage validation layer
2. DesignPage submit API call
3. ProductionPage highlight logic
4. Unit tests (6 new tests)
5. Build + lint gate
6. Deploy

**Files to modify:**
- `/opt/spaceos/frontend/joinerytech-portal/src/pages/DesignPage.tsx`
- `/opt/spaceos/frontend/joinerytech-portal/src/pages/ProductionPage.tsx`
- `/opt/spaceos/frontend/joinerytech-portal/src/pages/DesignPage.test.tsx`
- `/opt/spaceos/frontend/joinerytech-portal/src/pages/ProductionPage.test.tsx`

**Files to create:**
- None (all modifications to existing files)

**Commands:**
```bash
cd /opt/spaceos/frontend/joinerytech-portal
pnpm test -- DesignPage.test.tsx ProductionPage.test.tsx
pnpm build
pnpm lint
```

**Deploy:**
```bash
scp -r dist/* user@vps:/var/www/joinerytech.hu/
sudo systemctl reload nginx
```
