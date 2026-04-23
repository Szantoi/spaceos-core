---
id: MSG-PORTAL-012
from: root
to: portal
type: task
priority: medium
status: READ
ref: MSG-TESTER-021
created: 2026-04-19
---

# PORTAL-012 — Nesting Panel UX Improvement (BUG-017)

## TESTER Feedback (TESTER-021)

When user clicks on a cutting plan card (e.g., Draft plan), nesting panel attempts to load but fails with **404 Not Found**.

**Current UX Problems:**
- ❌ No loading spinner (user doesn't know loading is happening)
- ❌ No explicit error message (only silent "Nincs nesting adat")
- ❌ Console error invisible to user

---

## Required UX Improvements

### 1. Loading State
When nesting panel is fetching data:
```
GET /bff/cutting/sheets/{id}/nesting → pending
UI: Show spinner + "Nesting betöltésben..."
```

### 2. Error Handling

**If 404 (Draft plan, nesting not available):**
```
UI: "Ez a terv még Draft státuszban van. Nesting csak 
    Finalized terveknél érhető el. Véglegesítsd a tervet!"
```

**If other error (backend issue):**
```
UI: "Hiba a nesting adatok betöltésekor. Próbáld meg később."
```

### 3. Implementation

**File:** `design-portal/apps/joinerytech/src/pages/CuttingPlansPage.tsx` (or similar)

**Pattern:**
```typescript
const [nestingLoading, setNestingLoading] = useState(false);
const [nestingError, setNestingError] = useState(null);

const loadNesting = async (sheetId) => {
  setNestingLoading(true);
  setNestingError(null);
  try {
    const data = await fetch(`/bff/cutting/sheets/${sheetId}/nesting`);
    if (!data.ok) {
      if (data.status === 404) {
        setNestingError("Terv még Draft státuszban. Finalizálás szükséges.");
      } else {
        setNestingError("Betöltési hiba.");
      }
      return;
    }
    // render nesting
  } catch (err) {
    setNestingError("Hálózati hiba.");
  } finally {
    setNestingLoading(false);
  }
};

// Render:
{nestingLoading && <Spinner />}
{nestingError && <ErrorBanner message={nestingError} />}
{nestingData && <NestingPanel data={nestingData} />}
```

---

## DoD

- [ ] Loading spinner implemented (`nestingLoading` state)
- [ ] Error message for 404 (Draft plan)
- [ ] Error message for other errors
- [ ] 323/323 tests pass
- [ ] Outbox: MSG-PORTAL-012-DONE (UX improved)

---

## Priority

**MEDIUM** — UX improvement, not critical path (Soft Launch can proceed if nesting is optional)

**Note:** Diagnosis (BUG-017 root cause) is CUTTING-020's job. This task is UX only.

---

**Skill:** `/spaceos-portal` — React state management, error UI

**Status:** UNREAD — PORTAL terminal handles after PORTAL-011 (logout fix)
