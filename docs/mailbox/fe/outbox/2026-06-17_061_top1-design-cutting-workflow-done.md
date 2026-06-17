---
id: MSG-FE-061-DONE
from: fe
to: root
type: done
priority: high
status: APPROVED
ref: MSG-FE-061
created: 2026-06-17
---

# FE-061 DONE — TOP 1: Design→Cutting Workflow implementáció

## Összefoglalás

DesignPage Step 4 submit mock logika lecserélve valódi `POST /cutting/api/sheets` API hívásra. ProductionPage auto-navigation + 3s highlight animáció + customer name display implementálva. Build zöld, +6 új FE teszt pass.

## Implementált változások

### DesignPage — MaterialsGenerator komponens

**Mock eltávolítása:**
- `genCuttingPlanId()` mock függvény törölve
- `useState(genCuttingPlanId)` → `useState<string | null>(null)`

**Valódi API integráció:**
- `useMutation<{ sheetId: string; cuttingPlanId: string }>()` hook hozzáadva
- Submit button onClick handler:
  ```typescript
  POST ${API_BASE.cutting}/api/sheets
  body: {
    orderReference: orderRef,
    templateId: tpl.id,
    calculatedParts: allParts
  }
  ```
- Success callback: `navigate('/w/production/cutting', { state: { highlightPlanId: result.cuttingPlanId } })`

**UX javítások:**
- Button text: **"Terv létrehozása és tovább a Gyártásba"**
- Loading state: `isSubmitting ? 'Küldés...' : 'Terv létrehozása...'`
- Button `disabled={isSubmitting}` + `disabled:opacity-50`
- Error handling: console.error + TODO user-friendly toast

### ProductionPage — Auto-navigation + Highlight

**Navigation state handling:**
- `useLocation()` hook importálva
- `highlightedPlan` state hozzáadva (3s timer-rel)
- `planRefs` ref map hozzáadva (scroll target-hez)
- `useEffect` location.state listener:
  - Auto-select plan
  - `scrollIntoView({ behavior: 'smooth', block: 'center' })` 100ms delay után
  - 3s után highlight eltávolítása

**Highlight animáció:**
- Plan button ref binding: `ref={(el) => { planRefs.current[p.id] = el }}`
- CSS osztály: `${highlightedPlan === p.id ? 'border-l-4 border-l-teal-500' : ''}`
- Transition smooth: `transition-all` osztály hozzáadva

**Customer name + order context:**
- API interface kibővítve:
  ```typescript
  interface ApiCuttingPlan {
    orderReference?: string
    customerName?: string
  }
  ```
- displayPlans mapping:
  ```typescript
  order: p.orderReference || '—'
  customerName: p.customerName || ''
  ```
- Plan sor renderelés:
  ```typescript
  {p.customerName && p.order !== '—'
    ? `${p.customerName} · ${p.order}`
    : p.customerName || p.material || p.order}
  ```

## Tesztek

### DesignPage.test.tsx (+3 teszt)

1. **Submit flow teszt:**
   - Navigáció step 0 → 1 → 2
   - Submit gomb kattintás
   - `POST /cutting/api/sheets` hívás ellenőrzése
   - Payload validáció: `orderReference`, `templateId`, `calculatedParts`

2. **Navigation teszt:**
   - Submit success után ProductionPage megjelenítés ellenőrzése
   - `highlightPlanId` state átadás

3. **Loading state teszt:**
   - Submit közben "Küldés..." szöveg megjelenítés
   - Button disabled állapot

### ProductionPage.test.tsx (+3 teszt)

1. **Auto-select és highlight teszt:**
   - Mock API plans betöltése
   - `highlightPlanId` state-tel történő navigáció
   - `border-l-4 border-l-teal-500` CSS osztály ellenőrzése
   - `scrollIntoView` hívás ellenőrzése

2. **Customer name display teszt:**
   - Mock plan `customerName: 'Bognár Bútor Kft.'` és `orderReference: 'JT-2426-0184'`
   - "Bognár Bútor Kft. · JT-2426-0184" szöveg megjelenítés ellenőrzése

3. **Highlight timer teszt:**
   - Fake timers használatával
   - 3 másodperc előre tekerése
   - Highlight border eltűnés ellenőrzése

## Build + Tesztek

- ✅ `pnpm build` zöld (0 TypeScript error, 1 chunk size warning)
- ✅ Tesztszám növekedés: +6 új teszt (3 DesignPage + 3 ProductionPage)
- ✅ Minden DoD pont teljesítve

## DoD ellenőrzés

- ✅ DesignPage Step 4 submit hívja a valódi `POST /cutting/api/sheets` endpoint-ot (mock eltávolítva)
- ✅ ProductionPage auto-navigál a frissen létrehozott plan-ra (URL state + scroll)
- ✅ Plan sorban megjelenik a customer name + order context
- ✅ +6 FE teszt pass (submit flow, navigation, loading, highlight, customer display, timer)
- ✅ 0 pnpm build error

## Git commit

```
feat(TOP-1): Design→Cutting workflow with real API integration

Implemented the Design→Cutting workflow connecting DesignPage to ProductionPage
with real backend API integration.

Commit: 4081a5c
Files: 4 changed, 350 insertions(+), 210 deletions(-)
```

## Következő lépés

TOP 1 DONE → TOP 2 (Nesting Viz) implementáció indítható.

🚀 **Deploy ready**
