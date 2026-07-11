---
id: MSG-FRONTEND-013
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: CONSENSUS_TOP1-3_Design-Cutting-Nesting-Scheduling.md
created: 2026-06-22
content_hash: 82ff2d9d5c11c98af118cdbdc75d98d01d392efc6c5f5d535b9793107f045e25
---

# TOP 1: Design → Cutting Plan Workflow Integration

## Kontextus

**Doorstar use-case:** Amikor egy design elkészül (Step 4), a vágási terv automatikusan elküldhető a gyártásba. Jelenleg mock `cuttingPlanId` generálódik, de nem megy át a backend-be.

**Cél:** Design→Cutting workflow integráció — valódi API hívás, navigation, highlight a ProductionPage-en.

## Feladat

Integrálj a meglévő `POST /cutting/api/sheets` backend endpoint-tal, és adj hozzá auto-navigation + highlight funkciót.

### Implementációs scope

#### 1. DesignPage Step 4 Submit Logika

**Fájl:** `src/pages/DesignPage.tsx`

**Változtatások:**
- Lecserélni a mock `cuttingPlanId` generálást valódi API hívásra
- `useApi()` hook használata: `POST ${API_BASE.cutting}/api/sheets`
- Request body mapping:
  ```typescript
  {
    orderReference: currentOrderRef,
    lines: cuttingList.map(part => ({
      partName: part.name,
      materialType: part.material,
      widthMm: part.width,
      heightMm: part.height,
      thicknessMm: part.thickness,
      quantity: part.quantity,
      notes: part.notes || null
    }))
  }
  ```
- Success callback: `navigate('/w/production/cutting', { state: { highlightPlanId: response.sheetId } })`
- Error handling: toast notification (red) + fallback mock mode (ha 409/400 hiba)

#### 2. ProductionPage Auto-scroll + Highlight

**Fájl:** `src/pages/ProductionPage.tsx`

**Változtatások:**
- `useLocation()` hook: `state.highlightPlanId` olvasás
- Ha van `highlightPlanId`:
  - Auto-select: `setSelectedPlan(highlightPlanId)`
  - Scroll to row: `ref.current?.scrollIntoView({ behavior: 'smooth', block: 'center' })`
  - Visual highlight: 3s-ig `border-teal-500` animáció a kiválasztott sorban
- Plan sorban megjeleníteni: customer name + order context (design template back-link) — jelenleg üres

#### 3. Breadcrumb UI (opcionális, alacsony prioritás)

- DesignPage Step 4 button szöveg: "Terv létrehozása és tovább a Gyártásba"
- ProductionPage toast notification (3s): "Új vágási terv: {planName} • {templateName}"

### DoD

- [ ] DesignPage Step 4 submit hívja a valódi `POST /cutting/api/sheets` endpointot (mock eltávolítva)
- [ ] ProductionPage auto-navigál a frissen létrehozott plan-ra (URL state + scroll)
- [ ] Plan sorban megjelenik a customer name + order context
- [ ] +3 FE teszt pass:
  - `DesignPage.test.tsx`: mock API válasz, navigate hívás ellenőrzése
  - `ProductionPage.test.tsx`: location state alapú highlight, scroll ref ellenőrzése
- [ ] `pnpm test` pass (742+ tests)
- [ ] `pnpm build` 0 error

### Backend API ellenőrzés

**Endpoint:** ✅ READY — `POST /cutting/api/sheets`
**Status:** 931/931 teszt, deployed

**Request DTO ellenőrzés:**
```csharp
// backend/spaceos-modules-cutting/src/.../SubmitCuttingSheetCommand.cs
public sealed record CuttingLineInput(
    string PartName,
    string MaterialType,
    decimal WidthMm,
    decimal HeightMm,
    decimal ThicknessMm,
    int Quantity,
    string? Notes);
```

**Frontend mapping kötelező:** `cuttingList` state → `CuttingLineInput[]`

### Implementációs irányelvek

**Követendő minták:**
- Cutting API base URL: `API_BASE.cutting` (már létezik az app-ban)
- `useApi()` hook: error handling + loading state
- ProductionPage: már használ `useLocation()` hook-ot (cutting tab)
- Highlight animáció: Tailwind `animate-pulse` vagy custom keyframe

**Kockázatok:**
- ⚠️ `cuttingList` formátum nem egyezik az API elvárással → validation réteg kell a submit előtt
- ⚠️ Ha a `POST /cutting/api/sheets` hiba (409 duplicate, 400 validation) → user-friendly error message kell

### Kapcsolódó fájlok

- `frontend/joinerytech-portal/src/pages/DesignPage.tsx` (lines ~239, 279)
- `frontend/joinerytech-portal/src/pages/ProductionPage.tsx`
- `frontend/joinerytech-portal/src/hooks/useApi.ts`
- `frontend/joinerytech-portal/src/config/api.ts` (API_BASE)

### Becsült idő

**2-3 nap** (API integráció, navigation, highlight, 3 teszt)

---

**Priority:** HIGH — Doorstar workflow törött pont #1, azonnali üzleti érték
**Ref:** `/opt/spaceos/docs/tasks/archive/CONSENSUS_TOP1-3_Design-Cutting-Nesting-Scheduling.md`
